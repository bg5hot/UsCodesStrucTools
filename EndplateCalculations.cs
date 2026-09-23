using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class EndplateCalculations
    {
        // Resistance factors (AISC 358-16 Section 2.4.1)
        private const double PHI_D = 1.00;  // Ductile limit states
        private const double PHI_N = 0.90;  // Nonductile limit states
        private const double E_MODULUS = 29000.0; // ksi

        // Bolt grades: Fnt (nominal tensile), Fnv (nominal shear), Fu (minimum tensile)
        private static readonly Dictionary<string, (double Fnt, double Fnv, double Fu)> BoltGradeProps = new()
        {
            ["A325"] = (90.0, 54.0, 120.0),
            ["A490"] = (113.0, 68.0, 150.0),
            ["F1852"] = (105.0, 63.0, 125.0),
        };

        // Bolt gross areas (in^2) by diameter
        private static readonly Dictionary<double, double> BoltAreas = new()
        {
            [0.75] = 0.442,
            [0.875] = 0.601,
            [1.0] = 0.785,
            [1.125] = 0.994,
            [1.25] = 1.23,
            [1.375] = 1.48,
            [1.5] = 1.77,
        };

        #region Input / Output

        public class InputParameters
        {
            // Beam properties
            public double BeamD { get; set; }
            public double BeamBf { get; set; }
            public double BeamTf { get; set; }
            public double BeamTw { get; set; }
            public double BeamZx { get; set; }
            public double BeamFy { get; set; } = 50.0;
            public double BeamFu { get; set; } = 65.0;
            public double BeamRy { get; set; } = 1.1;

            // Column properties
            public double ColD { get; set; }
            public double ColBf { get; set; }
            public double ColTf { get; set; }
            public double ColTw { get; set; }
            public double ColZx { get; set; }
            public double ColFy { get; set; } = 50.0;
            public double ColFu { get; set; } = 65.0;
            public double ColRy { get; set; } = 1.1;

            // Connection type: "4E", "4ES", or "8ES"
            public string ConnectionType { get; set; } = "4E";

            // Bolt parameters
            public double BoltDb { get; set; } = 1.0;
            public string BoltGrade { get; set; } = "A325";

            // End-plate geometry
            public double PlateBp { get; set; }  // width
            public double PlateTp { get; set; }  // thickness
            public double G { get; set; } = 4.0; // gage
            public double Pfo { get; set; } = 1.25;
            public double Pfi { get; set; } = 1.5;
            public double Pb { get; set; } = 3.5; // 8ES only

            // End-plate material
            public double PlateFy { get; set; } = 50.0;
            public double PlateFu { get; set; } = 65.0;

            // Stiffener (4ES / 8ES)
            public double StiffenerTs { get; set; }
            public double StiffenerLst { get; set; }
            public double StiffenerFy { get; set; } = 50.0;

            // Design parameters
            public double Span { get; set; } = 360.0;
            public string SystemType { get; set; } = "SMF";

            // Loads
            public double LoadD { get; set; }
            public double LoadL { get; set; }
            public double LoadS { get; set; }
            public double F1 { get; set; } = 0.5;
            public double Vu { get; set; }
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            // Summary booleans
            public bool OverallPassed { get; set; }
            public bool BoltDiameterOK { get; set; }
            public bool PlateThicknessOK { get; set; }

            // Key results
            public double Mf { get; set; }
            public double Ffu { get; set; }
            public double DbReq { get; set; }
            public double TpReq { get; set; }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Calculate h_i distances from the compression flange centerline.
        /// Per AISC 358-16 Section 6.8.1 notation.
        /// </summary>
        private static (double h0, double h1, double h2, double h3, double h4) CalculateHiDistances(
            string connType, double d, double tf, double pfo, double pfi, double pb)
        {
            double d_ft = d - tf; // distance from compression flange CL to tension flange CL

            if (connType == "4E" || connType == "4ES")
            {
                double h0 = d_ft + pfo;
                double h1 = d_ft - pfi;
                return (h0, h1, 0, 0, 0);
            }
            else // 8ES
            {
                double h1 = d_ft + pfo + pb;
                double h2 = d_ft + pfo;
                double h3 = d_ft - pfi;
                double h4 = Math.Max(d_ft - pfi - pb, 0);
                return (0, h1, h2, h3, h4);
            }
        }

        /// <summary>
        /// Calculate yield line mechanism parameter Y_p per AISC 358-16 Tables 6.2-6.4.
        /// </summary>
        private static double CalculateYp(string connType, double bp, double g,
            double h0, double h1, double h2, double h3, double h4,
            double pfo, double pfi, double pb)
        {
            double s = 0.5 * Math.Sqrt(bp * g);
            double pfiEff = Math.Min(pfi, s);

            if (connType == "4E")
            {
                // Table 6.2 - Four-Bolt Extended Unstiffened
                double Yp = (bp / 2.0)
                    * (h1 * (1.0 / pfiEff + 1.0 / s) + h0 * (1.0 / pfo) - 0.5)
                    + (2.0 / g) * (h1 * (pfiEff + s));
                return Yp;
            }
            else if (connType == "4ES")
            {
                // Table 6.3 - Four-Bolt Extended Stiffened
                double de = pfo;
                double pf = pfiEff;

                if (de <= s)
                {
                    // Case 1 (d_e <= s)
                    return (bp / 2.0)
                        * (h1 * (1.0 / pf + 1.0 / s) + h0 * (1.0 / pf + 1.0 / (2.0 * s)))
                        + (2.0 / g) * (h1 * (pf + s) + h0 * (de + pf));
                }
                else
                {
                    // Case 2 (d_e > s)
                    return (bp / 2.0)
                        * (h1 * (1.0 / pf + 1.0 / s) + h0 * (1.0 / s + 1.0 / pf))
                        + (2.0 / g) * (h1 * (pf + s) + h0 * (s + pf));
                }
            }
            else // 8ES
            {
                // Table 6.4 - Eight-Bolt Extended Stiffened
                double de = pfo;

                if (de <= s)
                {
                    // Case 1 (d_e <= s)
                    return (bp / 2.0)
                        * (h1 * (1.0 / (2.0 * de))
                           + h2 * (1.0 / pfo)
                           + h3 * (1.0 / pfiEff)
                           + h4 * (1.0 / s))
                        + (2.0 / g)
                        * (h1 * (de + 3.0 * pb / 4.0)
                           + h2 * (pfo + pb / 4.0)
                           + h3 * (pfiEff + 3.0 * pb / 4.0)
                           + h4 * (s + pb / 4.0))
                        + g / 2.0;
                }
                else
                {
                    // Case 2 (d_e > s)
                    return (bp / 2.0)
                        * (h1 * (1.0 / s)
                           + h2 * (1.0 / pfo)
                           + h3 * (1.0 / pfiEff)
                           + h4 * (1.0 / s))
                        + (2.0 / g)
                        * (h1 * (s + pb / 4.0)
                           + h2 * (pfo + 3.0 * pb / 4.0)
                           + h3 * (pfiEff + pb / 4.0)
                           + h4 * (s + 3.0 * pb / 4.0))
                        + g / 2.0;
                }
            }
        }

        #endregion

        #region Main Calculate

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;

            // ---------- Validate inputs ----------
            if (input.BeamD <= 0 || input.BeamBf <= 0 || input.BeamTf <= 0 || input.BeamTw <= 0 || input.BeamZx <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid beam section properties.";
                return result;
            }
            if (input.ColD <= 0 || input.ColBf <= 0 || input.ColTf <= 0 || input.ColTw <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid column section properties.";
                return result;
            }
            if (input.BoltDb <= 0 || input.PlateBp <= 0 || input.PlateTp <= 0 || input.G <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid bolt / plate geometry.";
                return result;
            }
            if (input.Pfo <= 0 || input.Pfi <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Pitch distances pfo and pfi must be positive.";
                return result;
            }
            if (input.ConnectionType == "8ES" && input.Pb <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Pb must be positive for 8ES connections.";
                return result;
            }
            if (!BoltGradeProps.ContainsKey(input.BoltGrade))
            {
                result.IsValid = false;
                result.ErrorMessage = $"Unknown bolt grade: {input.BoltGrade}";
                return result;
            }

            string conn = input.ConnectionType;
            double d = input.BeamD;
            double bf = input.BeamBf;
            double tf = input.BeamTf;
            double tw = input.BeamTw;
            double Zx = input.BeamZx;
            double Fyb = input.BeamFy;
            double Fub = input.BeamFu;
            double Ry = input.BeamRy;

            double db = input.BoltDb;
            double bp = input.PlateBp;
            double tp = input.PlateTp;
            double g = input.G;
            double pfo = input.Pfo;
            double pfi = input.Pfi;
            double pb = input.Pb;

            bool allPassed = true;

            // ================================================================
            // STEP 1 - Calculate M_f  (Eq. 6.8-1)
            // ================================================================
            p.Add("==========================================================================");
            p.Add("  END-PLATE CONNECTION DESIGN VERIFICATION (AISC 358-16 Chapter 6)");
            p.Add("==========================================================================");
            p.Add("");
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 1: CALCULATE M_f");
            p.Add("--------------------------------------------------------------------------");

            // C_pr per Eq. 2.4-2
            double Cpr = Math.Min((Fyb + Fub) / (2.0 * Fyb), 1.2);
            p.Add($"  C_pr = min((Fy+Fu)/(2*Fy), 1.2) = min(({Fyb:F1}+{Fub:F1})/(2*{Fyb:F1}), 1.2) = {Cpr:F3}");

            // M_pr = C_pr * Ry * Fy * Zx
            double Mpr = Cpr * Ry * Fyb * Zx;
            p.Add($"  M_pr = C_pr * Ry * Fy * Zx = {Cpr:F3} * {Ry:F2} * {Fyb:F1} * {Zx:F1}");
            p.Add($"       = {Mpr:F0} kip-in ({Mpr / 12.0:F1} kip-ft)");

            // S_h distance
            double Sh;
            if (conn == "4E")
            {
                Sh = Math.Min(d / 2.0, 3.0 * bf);
                p.Add($"  S_h = min(d/2, 3*bf) = min({d / 2.0:F2}, {3.0 * bf:F2}) = {Sh:F2} in");
            }
            else
            {
                Sh = input.StiffenerLst + tp;
                p.Add($"  S_h = L_st + t_p = {input.StiffenerLst:F2} + {tp:F3} = {Sh:F2} in");
            }

            // Calculate V_u if not provided
            double Vu = input.Vu;
            if (Vu <= 0)
            {
                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;
                double Lh = input.Span - input.ColD - 2.0 * Sh;
                if (Lh > 0)
                {
                    Vu = 2.0 * Mpr / Lh + gravity / 2.0;
                    p.Add($"  V_u calculated: 2*M_pr/L_h + gravity/2 = {Vu:F1} kips");
                }
                else
                {
                    Vu = 0;
                    p.Add($"  V_u = 0 kips (not enough span data to auto-calculate)");
                }
            }
            else
            {
                p.Add($"  V_u = {Vu:F2} kips (user-specified)");
            }

            double Mf = Mpr + Vu * Sh;
            result.Mf = Mf;
            p.Add("");
            p.Add($"  M_f = M_pr + V_u * S_h = {Mpr:F0} + {Vu:F2} * {Sh:F2}");
            p.Add($"       = {Mf:F0} kip-in ({Mf / 12.0:F1} kip-ft)");
            p.Add("");

            // ================================================================
            // STEP 2 - Geometry summary
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 2: CONNECTION GEOMETRY");
            p.Add("--------------------------------------------------------------------------");
            p.Add($"  Connection type: {conn}");

            var (h0, h1, h2, h3, h4) = CalculateHiDistances(conn, d, tf, pfo, pfi, pb);

            if (conn == "4E" || conn == "4ES")
            {
                p.Add($"  h0 (outer row) = (d-tf)+pfo = {h0:F3} in");
                p.Add($"  h1 (inner row) = (d-tf)-pfi = {h1:F3} in");
            }
            else
            {
                p.Add($"  h1 (outermost) = (d-tf)+pfo+pb = {h1:F3} in");
                p.Add($"  h2             = (d-tf)+pfo    = {h2:F3} in");
                p.Add($"  h3             = (d-tf)-pfi    = {h3:F3} in");
                p.Add($"  h4 (innermost) = (d-tf)-pfi-pb = {h4:F3} in");
            }

            double s_yield = 0.5 * Math.Sqrt(bp * g);
            p.Add($"  s = 0.5*sqrt(bp*g) = 0.5*sqrt({bp:F2}*{g:F2}) = {s_yield:F3} in");
            p.Add("");

            // ================================================================
            // STEP 3 - Required bolt diameter (Eq. 6.8-3 / 6.8-4)
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 3: REQUIRED BOLT DIAMETER");
            p.Add("--------------------------------------------------------------------------");

            var (Fnt, Fnv, _) = BoltGradeProps[input.BoltGrade];
            p.Add($"  Bolt grade: {input.BoltGrade}, F_nt = {Fnt:F0} ksi");

            double hSum;
            string eqLabel;
            if (conn == "4E" || conn == "4ES")
            {
                hSum = h0 + h1;
                eqLabel = "6.8-3";
            }
            else
            {
                hSum = h1 + h2 + h3 + h4;
                eqLabel = "6.8-4";
            }

            double dbReq = Math.Sqrt(2.0 * Mf / (Math.PI * PHI_N * Fnt * hSum));
            result.DbReq = dbReq;

            p.Add($"  AISC 358-16 Eq. {eqLabel}:");
            p.Add($"  d_b,req = sqrt(2*M_f / (pi * phi_n * F_nt * sum(h_i)))");
            p.Add($"          = sqrt(2*{Mf:F0} / (pi * {PHI_N} * {Fnt:F0} * {hSum:F2}))");
            p.Add($"          = {dbReq:F3} in");
            p.Add("");

            bool boltOK = db >= dbReq;
            result.BoltDiameterOK = boltOK;
            if (boltOK)
            {
                p.Add($"  Selected d_b = {db:F3} in >= d_b,req = {dbReq:F3} in  =>  OK");
            }
            else
            {
                p.Add($"  Selected d_b = {db:F3} in < d_b,req = {dbReq:F3} in  =>  FAIL");
                allPassed = false;
            }
            p.Add("");

            // ================================================================
            // STEP 4 - Bolt selection summary
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 4: BOLT SELECTION");
            p.Add("--------------------------------------------------------------------------");
            p.Add($"  Selected bolt diameter: {db:F3} in");
            p.Add($"  Required bolt diameter: {dbReq:F3} in");

            double Ab;
            if (!BoltAreas.TryGetValue(db, out Ab))
            {
                Ab = Math.PI * db * db / 4.0;
                p.Add($"  Bolt area (calculated): {Ab:F3} in^2");
            }
            else
            {
                p.Add($"  Bolt area: {Ab:F3} in^2");
            }
            p.Add("");

            // ================================================================
            // STEP 5 - Required plate thickness (Eq. 6.8-5)
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 5: REQUIRED PLATE THICKNESS");
            p.Add("--------------------------------------------------------------------------");

            double Yp = CalculateYp(conn, bp, g, h0, h1, h2, h3, h4, pfo, pfi, pb);
            p.Add($"  Y_p = {Yp:F2} in  (per Table {(conn == "4E" ? "6.2" : conn == "4ES" ? "6.3" : "6.4")})");

            double Fyp = input.PlateFy; // Plate Fy
            double tpReq = Math.Sqrt(1.11 * Mf / (PHI_D * Fyp * Yp));
            result.TpReq = tpReq;

            p.Add($"  AISC 358-16 Eq. 6.8-5:");
            p.Add($"  t_p,req = sqrt(1.11*M_f / (phi_d * F_yp * Y_p))  [F_yp = {Fyp:F0} ksi]");
            p.Add($"          = sqrt(1.11*{Mf:F0} / ({PHI_D} * {Fyp:F0} * {Yp:F2}))");
            p.Add($"          = {tpReq:F3} in");
            p.Add("");

            bool plateOK = tp >= tpReq;
            result.PlateThicknessOK = plateOK;
            if (plateOK)
            {
                p.Add($"  Selected t_p = {tp:F3} in >= t_p,req = {tpReq:F3} in  =>  OK");
            }
            else
            {
                p.Add($"  Selected t_p = {tp:F3} in < t_p,req = {tpReq:F3} in  =>  FAIL");
                allPassed = false;
            }
            p.Add("");

            // ================================================================
            // STEP 6 - Plate selection summary
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 6: PLATE SELECTION");
            p.Add("--------------------------------------------------------------------------");
            p.Add($"  Selected plate thickness: {tp:F3} in");
            p.Add($"  Required plate thickness: {tpReq:F3} in");
            p.Add($"  Plate width (b_p): {bp:F2} in");
            p.Add("");

            // ================================================================
            // STEP 7 - Beam flange force (Eq. 6.8-6)
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 7: BEAM FLANGE FORCE");
            p.Add("--------------------------------------------------------------------------");

            double Ffu = Mf / (d - tf);
            result.Ffu = Ffu;

            p.Add($"  AISC 358-16 Eq. 6.8-6:");
            p.Add($"  F_fu = M_f / (d - t_f) = {Mf:F0} / ({d:F2} - {tf:F3})");
            p.Add($"       = {Ffu:F1} kips");
            p.Add($"  F_fu/2 = {Ffu / 2.0:F1} kips  (force per flange side)");
            p.Add("");

            // ================================================================
            // STEP 8 - Plate shear yielding check (4E only, Eq. 6.8-7)
            // ================================================================
            if (conn == "4E")
            {
                p.Add("--------------------------------------------------------------------------");
                p.Add("  STEP 8: PLATE SHEAR YIELDING CHECK (4E)");
                p.Add("--------------------------------------------------------------------------");

                double phi_d_Rn = PHI_D * 0.6 * Fyp * bp * tp;
                double force = Ffu / 2.0;

                p.Add($"  AISC 358-16 Eq. 6.8-7:");
                p.Add($"  phi_d * 0.6 * F_yp * b_p * t_p = {PHI_D} * 0.6 * {Fyp:F0} * {bp:F2} * {tp:F3}");
                p.Add($"                                 = {phi_d_Rn:F1} kips");
                p.Add($"  F_fu/2 = {force:F1} kips");
                p.Add($"  Utilization: {force / phi_d_Rn:F3}");
                p.Add("");

                if (force <= phi_d_Rn)
                {
                    p.Add("  => OK - Plate shear yielding strength is adequate");
                }
                else
                {
                    p.Add("  => FAIL - Increase plate thickness or width");
                    allPassed = false;
                }
                p.Add("");
            }
            else
            {
                p.Add("  (Step 8 skipped - stiffened connection)");
                p.Add("");
            }

            // ================================================================
            // STEP 9 - Plate shear rupture check (4E only, Eq. 6.8-8)
            // ================================================================
            if (conn == "4E")
            {
                p.Add("--------------------------------------------------------------------------");
                p.Add("  STEP 9: PLATE SHEAR RUPTURE CHECK (4E)");
                p.Add("--------------------------------------------------------------------------");

                double Fup = input.PlateFu; // Plate Fu
                double An = tp * (bp - 2.0 * (db + 0.0625));
                double phi_n_Rn = PHI_N * 0.6 * Fup * An;
                double force = Ffu / 2.0;

                p.Add($"  AISC 358-16 Eq. 6.8-8:");
                p.Add($"  A_n = t_p * (b_p - 2*(d_b + 1/16))");
                p.Add($"      = {tp:F3} * ({bp:F2} - 2*({db:F3} + 0.0625))");
                p.Add($"      = {An:F2} in^2");
                p.Add($"  phi_n * 0.6 * F_up * A_n = {PHI_N} * 0.6 * {Fup:F0} * {An:F2}");
                p.Add($"                          = {phi_n_Rn:F1} kips");
                p.Add($"  F_fu/2 = {force:F1} kips");
                p.Add($"  Utilization: {force / phi_n_Rn:F3}");
                p.Add("");

                if (force <= phi_n_Rn)
                {
                    p.Add("  => OK - Plate shear rupture strength is adequate");
                }
                else
                {
                    p.Add("  => FAIL - Increase plate thickness or width");
                    allPassed = false;
                }
                p.Add("");
            }
            else
            {
                p.Add("  (Step 9 skipped - stiffened connection)");
                p.Add("");
            }

            // ================================================================
            // STEP 10 - Stiffener design (4ES / 8ES, Eq. 6.8-9, 6.8-10)
            // ================================================================
            if (conn == "4ES" || conn == "8ES")
            {
                p.Add("--------------------------------------------------------------------------");
                p.Add("  STEP 10: STIFFENER DESIGN");
                p.Add("--------------------------------------------------------------------------");

                double Fys = input.StiffenerFy; // Stiffener Fy
                double tsMin = tw * (Fyb / Fys);
                double ts = input.StiffenerTs;

                p.Add($"  AISC 358-16 Eq. 6.8-9:");
                p.Add($"  t_s >= t_w * (F_yb / F_ys) = {tw:F3} * ({Fyb:F0}/{Fys:F0}) = {tsMin:F3} in");
                p.Add($"  Selected t_s = {ts:F3} in");
                p.Add("");

                // Slenderness check (Eq. 6.8-10)
                double Lst = input.StiffenerLst;
                if (ts > 0)
                {
                    double slenderness = Lst / ts;
                    double slendernessLimit = 0.56 * Math.Sqrt(E_MODULUS / Fys);

                    p.Add($"  Slenderness check (Eq. 6.8-10):");
                    p.Add($"  L_st / t_s = {Lst:F2} / {ts:F3} = {slenderness:F2}");
                    p.Add($"  Limit = 0.56*sqrt(E/F_ys) = 0.56*sqrt({E_MODULUS:F0}/{Fys:F0}) = {slendernessLimit:F2}");
                    p.Add("");

                    bool stiffenerOK = ts >= tsMin && slenderness <= slendernessLimit;
                    if (stiffenerOK)
                    {
                        p.Add("  => OK - Stiffener design is adequate");
                    }
                    else
                    {
                        p.Add("  => FAIL - Adjust stiffener thickness");
                        if (ts < tsMin) p.Add($"     t_s = {ts:F3} < {tsMin:F3} (required)");
                        if (slenderness > slendernessLimit) p.Add($"     L_st/t_s = {slenderness:F2} > {slendernessLimit:F2} (limit)");
                        allPassed = false;
                    }
                }
                else
                {
                    p.Add("  WARNING: Stiffener thickness not specified.");
                    allPassed = false;
                }
                p.Add("");
            }
            else
            {
                p.Add("  (Step 10 skipped - unstiffened connection)");
                p.Add("");
            }

            // ================================================================
            // STEP 11 - Bolt shear strength (Eq. 6.8-11)
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 11: BOLT SHEAR STRENGTH");
            p.Add("--------------------------------------------------------------------------");

            int nbCompression = 2;
            double phi_n_Rn_shear = PHI_N * nbCompression * Fnv * Ab;

            p.Add($"  AISC 358-16 Eq. 6.8-11:");
            p.Add($"  V_u <= phi_n * n_b * F_nv * A_b");
            p.Add($"  R_n = {PHI_N} * {nbCompression} * {Fnv:F0} * {Ab:F3} = {phi_n_Rn_shear:F1} kips");
            p.Add($"  V_u = {Vu:F2} kips");

            if (phi_n_Rn_shear > 0)
                p.Add($"  Utilization: {Vu / phi_n_Rn_shear:F3}");
            p.Add("");

            if (Vu <= phi_n_Rn_shear)
            {
                p.Add("  => OK - Bolt shear strength is adequate");
            }
            else
            {
                p.Add("  => FAIL - Increase bolt size or number of bolts");
                allPassed = false;
            }
            p.Add("");

            // ================================================================
            // STEP 12 - Bearing / Tearout check (simplified)
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 12: BEARING / TEAROUT CHECK (Simplified)");
            p.Add("--------------------------------------------------------------------------");

            double Fu_plate = input.PlateFu;
            double Fu_col = input.ColFu;
            double t_col = input.ColTf;
            double Lc_plate = g - db;
            double Lc_col = g - db;

            int ni = (conn == "8ES") ? 4 : 2;
            int no = (conn == "8ES") ? 4 : 2;

            double rni_plate = Math.Min(1.2 * Lc_plate * tp * Fu_plate, 2.4 * db * tp * Fu_plate);
            double rni_col = Math.Min(1.2 * Lc_col * t_col * Fu_col, 2.4 * db * t_col * Fu_col);

            double Lc_outer = Math.Max(pfo - db / 2.0, 0);
            double rno_plate = Math.Min(1.2 * Lc_outer * tp * Fu_plate, 2.4 * db * tp * Fu_plate);
            double rno_col = Math.Min(1.2 * Lc_outer * t_col * Fu_col, 2.4 * db * t_col * Fu_col);

            double Rn_bearing = PHI_N * (ni * (rni_plate + rni_col) + no * (rno_plate + rno_col));

            p.Add($"  Inner bolts (n={ni}):");
            p.Add($"    End plate: r_ni = {rni_plate:F1} kips");
            p.Add($"    Column flange: r_ni = {rni_col:F1} kips");
            p.Add($"  Outer bolts (n={no}):");
            p.Add($"    End plate: r_no = {rno_plate:F1} kips");
            p.Add($"    Column flange: r_no = {rno_col:F1} kips");
            p.Add($"  Total R_n = {Rn_bearing:F1} kips");
            p.Add($"  V_u = {Vu:F2} kips");

            if (Rn_bearing > 0)
                p.Add($"  Utilization: {Vu / Rn_bearing:F3}");
            p.Add("");

            if (Vu <= Rn_bearing)
            {
                p.Add("  => OK - Bearing/tearout strength is adequate");
            }
            else
            {
                p.Add("  => FAIL - Check bolt spacing and edge distances");
                allPassed = false;
            }
            p.Add("");

            // ================================================================
            // STEP 13 - Weld design (text)
            // ================================================================
            p.Add("--------------------------------------------------------------------------");
            p.Add("  STEP 13: WELD DESIGN");
            p.Add("--------------------------------------------------------------------------");
            p.Add("  Weld design per AISC 358-16 Section 6.7.6:");
            p.Add("  - Beam flange-to-end plate: CJP groove weld required");
            p.Add("  - Beam web-to-end plate: Sufficient to develop web strength");
            if (conn == "4ES" || conn == "8ES")
            {
                p.Add("  - Stiffener-to-beam flange: Fillet or CJP groove weld");
                p.Add("  - Stiffener-to-end plate: CJP groove weld required");
                p.Add($"    (Double fillet permitted if t_p <= 10 mm)");
            }
            p.Add("");

            // ================================================================
            // COLUMN SIDE - Column flange check (simplified)
            // ================================================================
            p.Add("==========================================================================");
            p.Add("  COLUMN-SIDE DESIGN");
            p.Add("==========================================================================");
            p.Add("");
            p.Add("--------------------------------------------------------------------------");
            p.Add("  COLUMN FLANGE FLEXURAL YIELDING (Simplified)");
            p.Add("--------------------------------------------------------------------------");
            p.Add("  AISC 358-16 Eq. 6.8-13:");
            p.Add($"  t_cf >= sqrt(1.11*M_f / (phi_d*F_yc*Y_c))");
            p.Add("  Note: Y_c calculation requires Table 6.5 or 6.6.");
            p.Add("        Using rough estimate for preliminary check.");
            p.Add("");

            double tcf = input.ColTf;
            double Fyc = input.ColFy;
            double tcfReqApprox = Math.Sqrt(Mf / (PHI_D * Fyc * 100.0));

            p.Add($"  Approximate required t_cf (rough): {tcfReqApprox:F3} in");
            p.Add($"  Actual t_cf: {tcf:F3} in");
            p.Add("");

            if (tcf >= tcfReqApprox)
            {
                p.Add("  => OK - Column flange thickness appears adequate (simplified check)");
                p.Add("     Verify with detailed Y_c calculation if needed.");
            }
            else
            {
                p.Add("  => WARNING - Column flange may be inadequate");
                p.Add("     Consider: larger column, continuity plates, or detailed analysis");
                // Column side warning does not fail beam-side design
            }
            p.Add("");

            p.Add("  Continuity Plates:");
            p.Add("  Check per AISC 358-16 Chapter 2");
            p.Add("  Required if column flange thickness is inadequate");
            p.Add("");

            // ================================================================
            // SUMMARY
            // ================================================================
            p.Add("==========================================================================");
            p.Add("  DESIGN VERIFICATION SUMMARY");
            p.Add("==========================================================================");
            p.Add("");
            p.Add("  KEY RESULTS:");
            p.Add($"    M_f     = {Mf:F0} kip-in ({Mf / 12.0:F1} kip-ft)");
            p.Add($"    F_fu    = {Ffu:F1} kips");
            p.Add($"    d_b,req = {dbReq:F3} in   (selected: {db:F3} in)  {(boltOK ? "OK" : "FAIL")}");
            p.Add($"    t_p,req = {tpReq:F3} in   (selected: {tp:F3} in)  {(plateOK ? "OK" : "FAIL")}");
            p.Add("");

            result.OverallPassed = allPassed;
            result.IsValid = true;

            if (allPassed)
            {
                p.Add("  >>> ALL CHECKS PASSED - END-PLATE CONNECTION DESIGN IS ADEQUATE <<<");
            }
            else
            {
                p.Add("  >>> SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN <<<");
            }
            p.Add("==========================================================================");

            return result;
        }

        #endregion
    }
}
