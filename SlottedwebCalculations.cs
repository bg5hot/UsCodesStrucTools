using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// SlottedWeb (SW) Connection Design Verification
    /// AISC 358-16 Chapter 14, Section 14.8 - Design Procedure (10 steps)
    ///
    /// SlottedWeb connections feature slots in the beam web parallel and adjacent
    /// to each flange. The beam flanges are welded to the column flange using CJP
    /// groove welds (demand critical). A shear plate is welded to both the column
    /// flange and the beam web. The plastic hinge forms at the end of the shear plate.
    ///
    /// Key characteristics:
    ///   - SMF only (not prequalified for IMF)
    ///   - Beam web slots separate flanges from web near the connection
    ///   - CJP groove welds for beam flanges (demand critical)
    ///   - Shear plate welded to column flange, bolted + fillet welded to beam web
    ///   - Plastic hinge at end of shear plate (S_h = l_p)
    ///   - Beam shear phi = 1.0 per Commentary C-14.8
    /// </summary>
    public static class SlottedwebCalculations
    {
        // ====================== CONSTANTS ======================
        public const double PHI_D = 1.00;    // Ductile limit states
        public const double PHI_N = 0.90;    // Nonductile limit states
        public const double PHI_V = 1.00;    // Beam shear per Commentary C-14.8 (13 cyclic tests)
        public const double E = 29000.0;     // Modulus of elasticity (ksi)

        public const double BEAM_MAX_DEPTH = 36.0;     // W36 max
        public const double BEAM_MAX_WEIGHT = 400.0;   // plf
        public const double BEAM_MAX_TF = 2.25;        // 2-1/4 in (57 mm)
        public const double BEAM_MIN_SPAN_DEPTH = 6.4;

        // ====================== DATA CLASSES ======================

        public class InputParameters
        {
            // Beam properties
            public string BeamDesignation = "";
            public double BeamD  = 24.0;    // Depth (in)
            public double BeamBf = 9.0;     // Flange width (in)
            public double BeamTf = 0.5;     // Flange thickness (in)
            public double BeamTw = 0.3;     // Web thickness (in)
            public double BeamZx = 200.0;   // Plastic section modulus (in^3)
            public double BeamFy = 50.0;    // ksi (A992)
            public double BeamFu = 65.0;    // ksi
            public double BeamRy = 1.1;
            public double BeamRt = 1.1;
            public double BeamT  = 0.0;     // Clear distance between flanges (d - 2k). 0 = approximate

            // Column properties
            public string ColDesignation = "";
            public double ColD  = 14.0;
            public double ColBf = 15.0;
            public double ColTf = 1.0;
            public double ColTw = 0.5;
            public double ColZx = 400.0;
            public double ColFy = 50.0;
            public double ColFu = 65.0;

            // Shear plate
            public double LpOverride = 0.0;  // Shear plate width (in). 0 = auto
            public double PlateFy = 50.0;    // ksi

            // Design parameters
            public double Span = 360.0;      // Center-to-center span (in)
            public double StoryAbove = 156.0;
            public double StoryBelow = 156.0;
            public double Pu = 0.0;          // Column axial load (kips)
            public double AsCol = 0.0;       // Column cross-section area (in^2, 0=approximate)

            // Gravity loads
            public double LoadD = 0.0;       // kips
            public double LoadL = 0.0;
            public double LoadS = 0.0;
            public double F1   = 0.5;        // Live load factor
        }

        public class DesignResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = "";
            public List<string> Process { get; set; } = new();

            public bool OverallPassed { get; set; }

            // Individual checks
            public bool PrequalificationPassed { get; set; }
            public bool ShearPlatePassed { get; set; }
            public bool BeamShearPassed { get; set; }
            public bool ContinuityPlatesPassed { get; set; }
            public bool PanelZonePassed { get; set; }
            public bool ColumnBeamRatioPassed { get; set; }

            // Key results
            public double Cpr { get; set; }
            public double Mpr { get; set; }     // kip-in
            public double Mf  { get; set; }     // kip-in
            public double Vbeam { get; set; }   // kips
            public double Ls { get; set; }      // Slot length (in)
            public double Lp { get; set; }      // Shear plate width (in)
            public double H  { get; set; }      // Shear plate height (in)
            public double Tp { get; set; }      // Shear plate thickness (in)
            public double Mweld { get; set; }   // kip-in
            public double Vweld { get; set; }   // kips
            public double Ex { get; set; }      // Weld eccentricity (in)
            public double Lb { get; set; }      // Clear span (in)

            // Utilization ratios
            public double BeamShearRatio { get; set; }
            public double PanelZoneRatio { get; set; }
            public double ColumnBeamRatio { get; set; }
        }

        // ====================== MAIN CALCULATE ======================

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;
            bool allPassed = true;

            try
            {
                // --- Input Summary ---
                p.Add("================================================================================");
                p.Add("  SLOTTEDWEB (SW) CONNECTION DESIGN VERIFICATION (AISC 358-16 CHAPTER 14)");
                p.Add("================================================================================");
                p.Add("");
                p.Add("--- INPUT PARAMETERS ---");

                // Compute T_eff
                double beamT = input.BeamT > 0 ? input.BeamT : input.BeamD - 2 * (input.BeamTf + input.BeamTw);
                string tStr = input.BeamT > 0 ? $"{input.BeamT:F2} (user)" : $"{beamT:F2} (approx: d-2*(tf+tw))";

                p.Add($"BEAM: {input.BeamDesignation} | d={input.BeamD:F2} bf={input.BeamBf:F2} tf={input.BeamTf:F3} tw={input.BeamTw:F3} Zx={input.BeamZx:F1}");
                p.Add($"      Fy={input.BeamFy} Fu={input.BeamFu} Ry={input.BeamRy} Rt={input.BeamRt} T={tStr}");
                p.Add($"COLUMN: {input.ColDesignation} | d={input.ColD:F2} bf={input.ColBf:F2} tf={input.ColTf:F3} tw={input.ColTw:F3} Zx={input.ColZx:F1}");
                p.Add($"        Fy={input.ColFy} Fu={input.ColFu}");

                double gravity = 1.2 * input.LoadD + input.F1 * input.LoadL + 0.2 * input.LoadS;
                p.Add($"SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | SMF");
                p.Add($"STORY: H_above={input.StoryAbove:F0} | H_below={input.StoryBelow:F0} | Pu={input.Pu:F0} kips");
                p.Add($"LOADS: D={input.LoadD} L={input.LoadL} S={input.LoadS} | Gravity={gravity:F2} kips");
                p.Add("");

                // ===== STEP 0: PREQUALIFICATION =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  PREQUALIFICATION LIMITS (SECTION 14.3)");
                p.Add("--------------------------------------------------------------------------------");

                bool prequal = true;
                double beamWeight = ParseWeight(input.BeamDesignation);

                // System type: SMF only
                p.Add("  System type: SMF: OK");

                // Beam depth <= 36
                p.Add($"  Beam depth: d = {input.BeamD:F2} in <= {BEAM_MAX_DEPTH:F0}: " + (input.BeamD <= BEAM_MAX_DEPTH ? "OK" : "FAIL"));
                if (input.BeamD > BEAM_MAX_DEPTH) prequal = false;

                // Beam weight <= 400
                p.Add($"  Beam weight: {beamWeight:F0} plf <= {BEAM_MAX_WEIGHT:F0}: " + (beamWeight <= BEAM_MAX_WEIGHT ? "OK" : "FAIL"));
                if (beamWeight > BEAM_MAX_WEIGHT) prequal = false;

                // Beam tf <= 2.25
                p.Add($"  Beam tf: {input.BeamTf:F3} in <= {BEAM_MAX_TF:F1}: " + (input.BeamTf <= BEAM_MAX_TF ? "OK" : "FAIL"));
                if (input.BeamTf > BEAM_MAX_TF) prequal = false;

                // Span/depth ratio >= 6.4
                double spanDepth = input.Span / input.BeamD;
                p.Add($"  Span/depth: L/d = {spanDepth:F1} >= {BEAM_MIN_SPAN_DEPTH:F1}: " + (spanDepth >= BEAM_MIN_SPAN_DEPTH ? "OK" : "FAIL"));
                if (spanDepth < BEAM_MIN_SPAN_DEPTH) prequal = false;

                // Column depth <= 36
                p.Add($"  Column depth: d = {input.ColD:F2} in <= 36.0 (W36 max): " + (input.ColD <= 36.0 ? "OK" : "FAIL"));
                if (input.ColD > 36.0) prequal = false;

                result.PrequalificationPassed = prequal;
                if (!prequal) allPassed = false;
                p.Add("");

                // ===== Computed quantities used across steps =====
                // l_b = half the clear span length per AISC 358-16 Symbols table
                double l_b = (input.Span - input.ColD) / 2.0;
                result.Lb = l_b;

                // C_pr per Section 2.4.3
                double Cpr = Math.Min((input.BeamFy + input.BeamFu) / (2 * input.BeamFy), 1.2);
                result.Cpr = Cpr;

                // M_pr (Eq. 2.4-1)
                double Mpr = Cpr * input.BeamRy * input.BeamFy * input.BeamZx;
                result.Mpr = Mpr;

                // Fye
                double Fye = input.BeamRy * input.BeamFy;

                // ===== STEP 1: BEAM WEB SLOT DESIGN =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 1: BEAM WEB SLOT DESIGN (EQ. 14.8-1 TO 14.8-4)");
                p.Add("--------------------------------------------------------------------------------");

                // Eq. 14.8-1: l_s = 1.5 * bf
                double ls1 = 1.5 * input.BeamBf;

                // Eq. 14.8-2: l_s = 0.60 * tf * sqrt(E / Fye)
                double ls2 = 0.60 * input.BeamTf * Math.Sqrt(E / Fye);

                // Eq. 14.8-3: l_s = d / 2
                double ls3 = input.BeamD / 2.0;

                // Eq. 14.8-4: l_s = l_p + (l_b - l_p)/10
                // Iterative seed: with l_p = l_s/3, solving gives l_s = l_b/7
                double ls4 = l_b / 7.0;

                p.Add($"  l_b (clear span) = (L - d_c)/2 = ({input.Span:F0} - {input.ColD:F2})/2 = {l_b:F1} in");
                p.Add($"  Fye = Ry * Fy = {input.BeamRy} * {input.BeamFy} = {Fye:F1} ksi");
                p.Add($"  Eq. 14.8-1: l_s = 1.5*bf = 1.5*{input.BeamBf:F2} = {ls1:F2} in");
                p.Add($"  Eq. 14.8-2: l_s = 0.60*tf*sqrt(E/Fye) = {ls2:F2} in");
                p.Add($"  Eq. 14.8-3: l_s = d/2 = {input.BeamD:F2}/2 = {ls3:F2} in");
                p.Add($"  Eq. 14.8-4: l_s = l_b/7 = {l_b:F1}/7 = {ls4:F2} in (iterative seed)");

                // l_s is the least of the four
                double l_s = Math.Min(Math.Min(ls1, ls2), Math.Min(ls3, ls4));

                p.Add($"  l_s = min({ls1:F2}, {ls2:F2}, {ls3:F2}, {ls4:F2}) = {l_s:F2} in");

                // Commentary C-14.8-1 check (informational)
                double lsOverTf = l_s / input.BeamTf;
                double limitCt = 0.60 * Math.Sqrt(E / input.BeamFy);
                bool ctOk = lsOverTf <= limitCt;
                p.Add($"  Cmt C-14.8-1: l_s/tf = {lsOverTf:F1} <= {limitCt:F1}: " + (ctOk ? "OK" : "WARNING"));

                // Protected zone extents (Section 14.3.1(8))
                double pzWeb = l_s + input.BeamD / 2;
                double pzFlange = l_s + input.BeamBf / 2;
                p.Add($"  Protected zone: web = column face to {pzWeb:F1} in (slot end + d/2)");
                p.Add($"  Protected zone: flange = column face to {pzFlange:F1} in (slot end + bf/2)");
                p.Add("");

                // ===== STEP 2: SHEAR PLATE DESIGN =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 2: SHEAR PLATE DESIGN (EQ. 14.8-5, 14.8-6)");
                p.Add("--------------------------------------------------------------------------------");

                // Shear plate width limits: l_s/3 <= l_p <= min(l_s/2, 6 in)
                double lpMin = l_s / 3.0;
                double lpMax = Math.Min(l_s / 2.0, 6.0);

                double l_p;
                if (input.LpOverride > 0)
                {
                    l_p = input.LpOverride;
                }
                else
                {
                    l_p = lpMax; // use maximum allowed width
                }

                p.Add($"  l_p limits: l_s/3 = {lpMin:F2} <= l_p <= min(l_s/2, 6) = {lpMax:F2} in");
                p.Add($"  Use l_p = {l_p:F2} in");

                bool lpOk = l_p >= lpMin && l_p <= lpMax;
                if (!lpOk)
                {
                    p.Add($"  WARNING: l_p = {l_p:F2} outside range [{lpMin:F2}, {lpMax:F2}]");
                }

                // Re-check Eq. 14.8-4 with actual l_p (iterative verification)
                double ls4Check = l_p + (l_b - l_p) / 10.0;
                if (ls4Check < l_s)
                {
                    p.Add($"  Eq. 14.8-4 re-check: l_p + (l_b-l_p)/10 = {l_p:F2} + ({l_b:F1}-{l_p:F2})/10 = {ls4Check:F2} < l_s = {l_s:F2} => GOVERNS");
                    l_s = ls4Check;
                }

                // Eq. 14.8-5: h = T - 2 in +/- 1 in
                double T_beam = beamT;
                double h = T_beam - 2.0;

                p.Add($"  T (clear distance) = {T_beam:F2} in");
                p.Add($"  h = T - 2 = {T_beam:F2} - 2 = {h:F2} in  (Eq. 14.8-5)");

                // Eq. 14.8-6: t_p = C_pr * (6/h^2) * Ry * (Zx * l_p / (l_b - l_p))
                double tpReq;
                if (h > 0 && (l_b - l_p) > 0)
                {
                    tpReq = Cpr * (6.0 / (h * h)) * input.BeamRy *
                            (input.BeamZx * l_p / (l_b - l_p));
                }
                else
                {
                    tpReq = 999;
                }

                // Minimum thickness: 2/3 * tw and 3/8 in
                double tpMinTw = 2.0 / 3.0 * input.BeamTw;
                double tpMin = Math.Max(tpMinTw, 0.375);
                double t_p = Math.Max(tpReq, tpMin);
                // Round up to nearest 1/8 in
                t_p = Math.Ceiling(t_p * 8) / 8;

                p.Add($"  C_pr = min((Fy+Fu)/(2*Fy), 1.2) = {Cpr:F3}");
                p.Add($"  t_p,req = C_pr*(6/h^2)*Ry*(Zx*l_p/(l_b-l_p))  (Eq. 14.8-6)");
                p.Add($"  t_p,req = {Cpr:F3}*(6/{h:F2}^2)*{input.BeamRy}*({input.BeamZx:F1}*{l_p:F2}/({l_b:F1}-{l_p:F2}))");
                p.Add($"  t_p,req = {tpReq:F3} in");
                p.Add($"  t_p,min = max(2/3*tw, 3/8) = max({tpMinTw:F3}, 0.375) = {tpMin:F3} in");
                p.Add($"  Use t_p = {t_p:F3} in (Fy = {input.PlateFy} ksi)");

                result.Ls = l_s;
                result.Lp = l_p;
                result.H = h;
                result.Tp = t_p;
                result.ShearPlatePassed = true;
                p.Add("");

                // ===== STEP 3: SHEAR PLATE-TO-BEAM WEB WELD =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 3: SHEAR PLATE-TO-BEAM WEB WELD (EQ. 14.8-7 TO 14.8-11)");
                p.Add("--------------------------------------------------------------------------------");

                // V_beam (Eq. 14.8-10)
                double Vgravity = gravity / 2.0;
                if (l_b <= l_p)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"l_b ({l_b:F1}) must be > l_p ({l_p:F2})";
                    p.Add($"ERROR: {result.ErrorMessage}");
                    return result;
                }
                double Vbeam = Mpr / (l_b - l_p) + Vgravity;

                p.Add($"  C_pr = {Cpr:F3}");
                p.Add($"  M_pr = C_pr * Ry * Fy * Zx = {Mpr:F0} kip-in");
                p.Add($"  V_beam = M_pr/(l_b - l_p) + V_gravity  (Eq. 14.8-10)");
                p.Add($"  V_beam = {Mpr:F0}/({l_b:F1}-{l_p:F2}) + {Vgravity:F2} = {Vbeam:F1} kips");

                // Z_web (Eq. 14.8-11)
                double Zweb = input.BeamTw * T_beam * T_beam / 4.0;

                p.Add($"  Z_web = tw*T^2/4 = {input.BeamTw:F3}*{T_beam:F2}^2/4 = {Zweb:F2} in^3  (Eq. 14.8-11)");

                // M_weld (Eq. 14.8-7)
                double tpTotal = t_p + input.BeamTw;
                double Mweld = Cpr * (t_p / tpTotal) * Math.Pow(h / T_beam, 2) *
                               Zweb * input.BeamRy * input.BeamFy;

                // V_weld (Eq. 14.8-8)
                double Vweld = Vbeam * (t_p / tpTotal);

                // e_x (Eq. 14.8-9)
                double ex = Vweld > 0 ? Mweld / Vweld : 0;

                p.Add("");
                p.Add($"  M_weld = C_pr*(t_p/(t_p+tw))*(h/T)^2*Z_web*Ry*Fy  (Eq. 14.8-7)");
                p.Add($"  M_weld = {Cpr:F3}*({t_p:F3}/{tpTotal:F3})*({h:F2}/{T_beam:F2})^2*{Zweb:F2}*{input.BeamRy}*{input.BeamFy}");
                p.Add($"  M_weld = {Mweld:F0} kip-in");
                p.Add($"  V_weld = V_beam*(t_p/(t_p+tw))  (Eq. 14.8-8)");
                p.Add($"  V_weld = {Vbeam:F1}*({t_p:F3}/{tpTotal:F3}) = {Vweld:F1} kips");
                p.Add($"  e_x = M_weld/V_weld = {Mweld:F0}/{Vweld:F1} = {ex:F2} in  (Eq. 14.8-9)");
                p.Add($"  Note: Design fillet weld group per AISC Manual Tables using e_x = {ex:F2} in");

                result.Vbeam = Vbeam;
                result.Mweld = Mweld;
                result.Vweld = Vweld;
                result.Ex = ex;
                p.Add("");

                // ===== STEP 4: SHEAR PLATE-TO-COLUMN FLANGE WELD =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 4: SHEAR PLATE-TO-COLUMN FLANGE WELD");
                p.Add("--------------------------------------------------------------------------------");

                p.Add("  Required weld strength = nominal strength of Step 3 weld group");
                p.Add($"  M_weld = {Mweld:F0} kip-in");
                p.Add($"  V_weld = {Vweld:F1} kips");
                p.Add("  Weld per AISC Specification (CJP, PJP, or fillet)");
                p.Add("  [Weld detailing per Section 14.6]");
                p.Add("");

                // ===== STEP 5: ERECTION BOLTS =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 5: ERECTION BOLTS (SECTION 14.8 STEP 5)");
                p.Add("--------------------------------------------------------------------------------");

                // Bolt diameter >= beam web thickness
                double dBoltMin = input.BeamTw;
                double boltDia = Math.Max(0.625, Math.Ceiling(dBoltMin * 8) / 8);

                // Max spacing 6 in o.c.
                int nBoltsMin = (int)Math.Ceiling(h / 6.0);

                p.Add($"  Bolt diameter >= tw = {input.BeamTw:F3} in, use {boltDia:F3} in");
                p.Add($"  Max spacing = 6 in o.c. over plate height h = {h:F2} in");
                p.Add($"  Minimum bolts = ceil({h:F2}/6) = {nBoltsMin}");
                p.Add("  Pretensioned high-strength bolts in standard holes");
                p.Add("");

                // ===== STEP 6: MOMENT AT COLUMN FACE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 6: MOMENT AT COLUMN FACE (EQ. 14.8-12)");
                p.Add("--------------------------------------------------------------------------------");

                // Eq. 14.8-12
                double Mf = Mpr + Vbeam * l_p;

                p.Add($"  M_f = M_pr + V_beam * l_p  (Eq. 14.8-12)");
                p.Add($"  M_f = {Mpr:F0} + {Vbeam:F1} * {l_p:F2} = {Mf:F0} kip-in ({Mf / 12:F1} kip-ft)");

                result.Mf = Mf;
                p.Add("");

                // ===== STEP 7: BEAM SHEAR STRENGTH =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 7: BEAM SHEAR STRENGTH (AISC 360 G2.1)");
                p.Add("--------------------------------------------------------------------------------");

                // Per Commentary C-14.8: phi = 1.0, Cv = 1.0 (based on 13 cyclic tests)
                double Aw = input.BeamD * input.BeamTw;
                double Cv = 1.0;
                double Vn = 0.6 * input.BeamFy * Aw * Cv;
                double phiVn = PHI_V * Vn;

                bool beamShearOK = Vbeam <= phiVn;
                p.Add($"  phi = {PHI_V} (per Commentary C-14.8), Cv = 1.0");
                p.Add($"  V_n = 0.6*Fy*d*tw = 0.6*{input.BeamFy}*{input.BeamD:F2}*{input.BeamTw:F3} = {Vn:F1} kips");
                p.Add($"  phi*V_n = {phiVn:F1} kips >= V_beam = {Vbeam:F1}: " + (beamShearOK ? "OK" : "FAIL"));

                result.BeamShearPassed = beamShearOK;
                result.BeamShearRatio = phiVn > 0 ? Vbeam / phiVn : 999;
                if (!beamShearOK) allPassed = false;
                p.Add("");

                // ===== STEP 8: CONTINUITY PLATES =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 8: CONTINUITY PLATES (SECTION 2.4.4)");
                p.Add("--------------------------------------------------------------------------------");

                // Flange force from M_f
                double Ff = Mf / (input.BeamD - input.BeamTf);

                // AISC 360 J10.1: Flange local bending
                double tcfReq = Math.Sqrt(Ff / (PHI_N * 6.25 * input.ColFy));

                // Web local yielding check (AISC 360 J10.2)
                double k = input.ColTf;
                double twReqWy = k > 0 ? Ff / (PHI_D * 5 * input.ColFy * k) : 999;

                bool needPlates = input.ColTf < tcfReq || input.ColTw < twReqWy;

                p.Add($"  F_f = M_f/(d-tf) = {Mf:F0}/({input.BeamD:F2}-{input.BeamTf:F3}) = {Ff:F1} kips");
                p.Add($"  Flange local bending: t_fc >= {tcfReq:F3} in (actual {input.ColTf:F3})");
                p.Add($"  Web local yielding: t_wc >= {twReqWy:F3} in (actual {input.ColTw:F3})");

                if (needPlates)
                {
                    double tsMin = Math.Max(input.ColTw, 0.5 * input.BeamTf);
                    p.Add($"  => Continuity plates REQUIRED (ts >= {tsMin:F3} in)");
                }
                else
                {
                    p.Add("  => Continuity plates not required by calculation");
                }

                result.ContinuityPlatesPassed = true; // plates will be provided if needed
                p.Add("");

                // ===== STEP 9: PANEL ZONE =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 9: PANEL ZONE CHECK (SECTION 14.4)");
                p.Add("--------------------------------------------------------------------------------");

                double dc = input.ColD; // overall column depth per AISC 360 J10.6

                // Panel zone demand from column face moments
                double sumMface = 2 * Mf;
                double Hstory = (input.StoryAbove + input.StoryBelow) / 2.0;
                double Vcol = Hstory > 0 ? sumMface / Hstory : 0;
                double Vpz = sumMface / (input.BeamD - input.BeamTf) - Vcol;

                // Panel zone capacity per AISC 360 J10.6
                double phiPz = 1.0; // per AISC 341
                double phiRnPz = phiPz * 0.6 * input.ColFy * dc * input.ColTw;

                bool pzOk = phiRnPz >= Vpz;

                p.Add($"  Sum M_face = 2*M_f = {sumMface:F0} kip-in");
                p.Add($"  V_col = {sumMface:F0}/{Hstory:F0} = {Vcol:F1} kips");
                p.Add($"  V_pz = {sumMface:F0}/({input.BeamD:F2}-{input.BeamTf:F3}) - {Vcol:F1} = {Vpz:F1} kips");
                p.Add($"  phi*R_n = 1.0*0.6*{input.ColFy}*{dc:F2}*{input.ColTw:F3} = {phiRnPz:F1} kips");
                p.Add($"  phi*R_n = {phiRnPz:F1} >= V_pz = {Vpz:F1}: " + (pzOk ? "OK" : "FAIL - need doubler plates"));

                result.PanelZonePassed = pzOk;
                result.PanelZoneRatio = phiRnPz > 0 ? Vpz / phiRnPz : 999;
                if (!pzOk) allPassed = false;
                p.Add("");

                // ===== STEP 10: COLUMN-BEAM MOMENT RATIO =====
                p.Add("--------------------------------------------------------------------------------");
                p.Add("  STEP 10: COLUMN-BEAM MOMENT RATIO (EQ. 14.4-1)");
                p.Add("--------------------------------------------------------------------------------");

                // M_uv per Eq. 14.4-1
                double Muv = Vbeam * (l_p + input.ColD / 2.0);

                // Sum M_pb* for beams (2 beams, one each side)
                double MpbStar = Mpr + Muv;
                int nBeams = 2;
                double sumMpb = nBeams * MpbStar;

                // Column moment capacity (with axial load)
                double AsCol = input.AsCol;
                if (AsCol <= 0)
                {
                    AsCol = input.ColBf * input.ColTf * 2 + (input.ColD - 2 * input.ColTf) * input.ColTw;
                }

                double denom = AsCol * input.ColFy;
                double Mpc;
                if (denom > 0 && input.Pu > 0)
                {
                    Mpc = input.ColZx * input.ColFy * Math.Max(0, 1 - input.Pu / denom);
                }
                else
                {
                    Mpc = input.ColZx * input.ColFy;
                }

                double sumMpc = 2 * Mpc;

                double ratio = sumMpb > 0 ? sumMpc / sumMpb : 999;
                bool cbPassed = ratio >= 1.0;

                p.Add($"  M_uv = V_beam*(l_p + d_c/2)  (Eq. 14.4-1)");
                p.Add($"  M_uv = {Vbeam:F1}*({l_p:F2} + {input.ColD / 2:F2}) = {Muv:F0} kip-in");
                p.Add($"  M_pb* = M_pr + M_uv = {Mpr:F0} + {Muv:F0} = {MpbStar:F0} kip-in");
                p.Add($"  Sum M_pb* = {nBeams} * {MpbStar:F0} = {sumMpb:F0} kip-in");
                p.Add($"  M_pc = Zx_c * Fy_c * (1 - Pu/(As*Fy))");
                p.Add($"  M_pc = {input.ColZx:F1}*{input.ColFy}*(1 - {input.Pu:F0}/{denom:F0}) = {Mpc:F0} kip-in");
                p.Add($"  Sum M_pc = 2 * {Mpc:F0} = {sumMpc:F0} kip-in");
                p.Add($"  Ratio = {sumMpc:F0} / {sumMpb:F0} = {ratio:F3}");
                if (!cbPassed)
                {
                    p.Add("  FAIL - Increase column size");
                }
                else
                {
                    p.Add("  OK");
                }
                p.Add($"  Note: Simplified (same column above/below, {nBeams} beams)");

                result.ColumnBeamRatioPassed = cbPassed;
                result.ColumnBeamRatio = ratio;
                if (!cbPassed) allPassed = false;
                p.Add("");

                // ===== SUMMARY =====
                p.Add("================================================================================");
                p.Add("  DESIGN VERIFICATION SUMMARY");
                p.Add("================================================================================");

                p.Add($"Prequalification:       {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
                p.Add($"Shear plate (Step 2):   {(result.ShearPlatePassed ? "PASS" : "FAIL")}");
                p.Add($"Beam shear (Step 7):    {(result.BeamShearPassed ? "PASS" : "FAIL")}");
                p.Add($"Continuity plates (S8): {(result.ContinuityPlatesPassed ? "PASS" : "FAIL")}");
                p.Add($"Panel zone (Step 9):    {(result.PanelZonePassed ? "PASS" : "FAIL")}");
                p.Add($"Column-beam ratio (S10):{(result.ColumnBeamRatioPassed ? "PASS" : "FAIL")}");
                p.Add("");
                p.Add("KEY RESULTS:");
                p.Add($"  M_pr = {Mpr:F0} kip-in | M_f = {Mf:F0} kip-in");
                p.Add($"  V_beam = {Vbeam:F1} kips");
                p.Add($"  Slot length: l_s = {l_s:F2} in");
                p.Add($"  Shear plate: l_p = {l_p:F2} | h = {h:F2} | t_p = {t_p:F3} in");
                p.Add($"  Weld forces: M_weld = {Mweld:F0} kip-in | V_weld = {Vweld:F1} kips | e_x = {ex:F2} in");
                p.Add($"  Clear span: l_b = {l_b:F1} in");
                p.Add("");

                p.Add("================================================================================");
                if (allPassed)
                    p.Add("  ALL CHECKS PASSED");
                else
                    p.Add("  SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN");
                p.Add("================================================================================");

                result.OverallPassed = allPassed;
                result.IsValid = true;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Calculation error: {ex.Message}";
                p.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        // ====================== HELPER METHODS ======================

        private static double ParseWeight(string designation)
        {
            try
            {
                var parts = designation.ToUpper().Split('X');
                if (parts.Length > 1 && double.TryParse(parts[1], out double w))
                    return w;
            }
            catch { }
            return 999; // Unknown, will trigger check
        }
    }
}
