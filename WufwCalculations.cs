using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class WufwCalculations
    {
        // Resistance factors
        private const double PHI_D = 1.00;   // Ductile limit states (AISC 358 Section 2.4.1)
        private const double PHI_V = 1.00;   // Shear (AISC 360 G2.1(a), phi=1.0 for rolled W-shapes)

        // Steel properties
        private const double E = 29000.0;    // Modulus of elasticity (ksi)

        // WUF-W specific constants
        private const double CPR_WUFW = 1.4; // C_pr for WUF-W per Section 8.7 Step 1 (NOT Eq. 2.4-2)
        private const double SH_WUFW = 0.0;  // S_h for WUF-W per Section 8.7 Step 2 (at column face)

        public class InputParameters
        {
            // Beam properties
            public double BeamD = 0;       // Depth (in)
            public double BeamBf = 0;      // Flange width (in)
            public double BeamTf = 0;      // Flange thickness (in)
            public double BeamTw = 0;      // Web thickness (in)
            public double BeamZx = 0;      // Plastic section modulus (in^3)
            public double BeamFy = 50.0;   // ksi
            public double BeamFu = 65.0;   // ksi
            public double BeamRy = 1.1;
            public string BeamName = "";

            // Column properties
            public double ColD = 0;
            public double ColBf = 0;
            public double ColTf = 0;
            public double ColTw = 0;
            public double ColZx = 0;
            public double ColFy = 50.0;
            public double ColFu = 65.0;
            public double ColRy = 1.1;
            public string ColName = "";

            // Design parameters
            public double Span = 0;           // Beam span (in)
            public string SystemType = "SMF"; // "SMF" or "IMF"

            // Loads
            public double D = 0;       // Total dead load on span (kips)
            public double L = 0;       // Total live load on span (kips)
            public double S = 0;       // Total snow load on span (kips)
            public double f1 = 0.5;    // Live load factor
            public double Vu = 0;      // User-specified required shear (kips)
        }

        public class DesignResult
        {
            public bool IsValid = false;
            public string ErrorMessage = "";
            public List<string> Process = new();

            public bool OverallPassed = false;

            // Key results
            public double Mpr = 0;           // Probable maximum moment (kip-in)
            public double Vh = 0;            // Shear at plastic hinge (kips)
            public double Vu = 0;            // Design shear (kips)
            public double ShearRatio = 0;    // Vu / phi*Vn
            public double ColumnBeamRatio = 0;
            public double PanelZoneRatio = 0;

            // Individual checks
            public bool PrequalificationPassed = false;
            public bool ColumnBeamPassed = false;
            public bool BeamShearPassed = false;
            public bool ContinuityPlatesPassed = false;
            public bool PanelZonePassed = false;
            public bool ConnectionDetailsPassed = true; // prescriptive
        }

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var p = result.Process;

            // ---- Input validation ----
            if (input.BeamD <= 0 || input.BeamBf <= 0 || input.BeamTf <= 0 ||
                input.BeamTw <= 0 || input.BeamZx <= 0)
            {
                result.ErrorMessage = "Invalid beam section properties. Select a valid beam shape.";
                return result;
            }
            if (input.ColD <= 0 || input.ColBf <= 0 || input.ColTf <= 0 ||
                input.ColTw <= 0 || input.ColZx <= 0)
            {
                result.ErrorMessage = "Invalid column section properties. Select a valid column shape.";
                return result;
            }
            if (input.Span <= 0)
            {
                result.ErrorMessage = "Span must be a positive value.";
                return result;
            }
            if (input.BeamFy <= 0 || input.ColFy <= 0)
            {
                result.ErrorMessage = "Fy must be positive for beam and column.";
                return result;
            }
            if (input.SystemType != "SMF" && input.SystemType != "IMF")
            {
                result.ErrorMessage = "System type must be SMF or IMF.";
                return result;
            }

            // ---- Header ----
            p.Add("================================================================================");
            p.Add("  WUF-W CONNECTION DESIGN VERIFICATION (AISC 358-16 CHAPTER 8)");
            p.Add("================================================================================");
            p.Add("");

            // ---- Input Summary ----
            p.Add("================================================================================");
            p.Add("  INPUT PARAMETERS");
            p.Add("--------------------------------------------------------------------------------");
            p.Add($"  BEAM: {input.BeamName} | d={input.BeamD:F2}  bf={input.BeamBf:F2}  tf={input.BeamTf:F3}  tw={input.BeamTw:F3}  Zx={input.BeamZx:F1}");
            p.Add($"        Fy={input.BeamFy} ksi  Fu={input.BeamFu} ksi  Ry={input.BeamRy}");
            p.Add($"  COLUMN: {input.ColName} | d={input.ColD:F2}  bf={input.ColBf:F2}  tf={input.ColTf:F3}  tw={input.ColTw:F3}  Zx={input.ColZx:F1}");
            p.Add($"          Fy={input.ColFy} ksi  Fu={input.ColFu} ksi  Ry={input.ColRy}");
            double Lh = input.Span - input.ColD;
            p.Add($"  SPAN: L={input.Span:F0} in ({input.Span / 12:F1} ft) | {input.SystemType}");
            p.Add($"        L_h = L - d_c = {input.Span:F0} - {input.ColD:F2} = {Lh:F1} in ({Lh / 12:F1} ft)");
            p.Add($"        C_pr = {CPR_WUFW} (WUF-W specific) | S_h = {SH_WUFW} in");
            p.Add($"  LOADS: D={input.D}  L={input.L}  S={input.S}  f1={input.f1}  Vu={input.Vu:F2}");
            p.Add("");

            // ---- Step 0: Prequalification Limits ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  PREQUALIFICATION LIMITS (SECTION 8.3)");
            p.Add("--------------------------------------------------------------------------------");

            bool prequalPassed = true;
            string st = input.SystemType;

            // Beam depth <= 36 in
            bool beamDepthOK = input.BeamD <= 36.0;
            p.Add($"  Beam depth: d = {input.BeamD:F1} in <= 36 in: {(beamDepthOK ? "OK" : "FAIL")}");
            if (!beamDepthOK) prequalPassed = false;

            // Beam weight <= 150 plf
            double beamWeight = GetWeight(input.BeamName);
            bool beamWeightOK = beamWeight <= 150.0;
            p.Add($"  Beam weight: {beamWeight:F0} plf <= 150 plf: {(beamWeightOK ? "OK" : "FAIL")}");
            if (!beamWeightOK) prequalPassed = false;

            // Flange thickness <= 1.0 in
            bool tfOK = input.BeamTf <= 1.0;
            p.Add($"  Flange thickness: tf = {input.BeamTf:F3} in <= 1.0 in: {(tfOK ? "OK" : "FAIL")}");
            if (!tfOK) prequalPassed = false;

            // Span/depth ratio
            double sd = input.Span / input.BeamD;
            double sdMin = st == "SMF" ? 7.0 : 5.0;
            bool sdOK = sd >= sdMin;
            p.Add($"  Span/depth L/d = {sd:F1} >= {sdMin:F0} ({st}): {(sdOK ? "OK" : "FAIL")}");
            if (!sdOK) prequalPassed = false;

            // Column depth <= 36 in
            bool colDepthOK = input.ColD <= 36.0;
            p.Add($"  Column depth: d_c = {input.ColD:F1} in <= 36 in: {(colDepthOK ? "OK" : "FAIL")}");
            if (!colDepthOK) prequalPassed = false;

            // C_pr check
            p.Add($"  C_pr = {CPR_WUFW} (WUF-W: must be 1.4): OK");
            p.Add($"  Lateral bracing required at d to 1.5d from column face");
            p.Add($"    = {input.BeamD:F1} to {1.5 * input.BeamD:F1} in from column face");
            p.Add($"  Protected zone: column face to d = {input.BeamD:F1} in from column face");
            p.Add("");
            result.PrequalificationPassed = prequalPassed;

            // ---- Step 1: M_pr ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 1: PROBABLE MAXIMUM MOMENT M_pr (SECTION 8.7)");
            p.Add("--------------------------------------------------------------------------------");

            double Cpr = CPR_WUFW;
            double Mpr = Cpr * input.BeamRy * input.BeamFy * input.BeamZx;
            p.Add($"  WUF-W specific: C_pr = {Cpr} (per Section 8.7 Step 1)");
            p.Add($"  Z_e = Z_x = {input.BeamZx:F1} in^3 (no reduction)");
            p.Add($"  M_pr = C_pr * Ry * Fy * Zx");
            p.Add($"  M_pr = {Cpr} * {input.BeamRy} * {input.BeamFy} * {input.BeamZx:F1}");
            p.Add($"  M_pr = {Mpr:F0} kip-in ({Mpr / 12:F1} kip-ft)");
            p.Add("");
            result.Mpr = Mpr;

            // ---- Step 2: S_h ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 2: PLASTIC HINGE LOCATION S_h (SECTION 8.7)");
            p.Add("--------------------------------------------------------------------------------");

            p.Add($"  WUF-W specific: S_h = 0 (plastic hinge at column face)");
            p.Add($"  Therefore: M_f = M_pr = {Mpr:F0} kip-in");
            p.Add($"  (No moment amplification from shear at S_h)");
            p.Add("");

            // ---- Step 3: V_h ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 3: SHEAR FORCE AT PLASTIC HINGE (SECTION 8.7)");
            p.Add("--------------------------------------------------------------------------------");

            double gravity = 1.2 * input.D + input.f1 * input.L + 0.2 * input.S;
            double Vh = 2.0 * Mpr / Lh + gravity / 2.0;
            double Vu = Math.Max(Vh, input.Vu);

            p.Add($"  L_h = {Lh:F1} in ({Lh / 12:F1} ft)");
            p.Add($"  Gravity load (1.2D + {input.f1}L + 0.2S) = {gravity:F2} kips (total on span)");
            p.Add($"  V_h = 2*M_pr/L_h + gravity/2");
            p.Add($"  V_h = 2*{Mpr:F0}/{Lh:F1} + {gravity:F2}/2");
            p.Add($"  V_h = {Vh:F1} kips");
            p.Add($"  V_u = max(V_h, user Vu) = max({Vh:F1}, {input.Vu:F2}) = {Vu:F1} kips");
            p.Add("");
            result.Vh = Vh;
            result.Vu = Vu;

            // ---- Step 4: Column-Beam Relationship ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 4: COLUMN-BEAM RELATIONSHIP (SECTION 8.4)");
            p.Add("--------------------------------------------------------------------------------");

            // Beam flange force
            double Ff = Mpr / (input.BeamD - input.BeamTf);
            p.Add($"  Beam flange force: F_f = M_pr / (d - tf)");
            p.Add($"  F_f = {Mpr:F0} / ({input.BeamD:F2} - {input.BeamTf:F3}) = {Ff:F1} kips");
            p.Add("");

            bool colBeamPassed = true;
            if (st == "SMF")
            {
                p.Add("  Strong-Column / Weak-Beam Check (AISC 341 Section E3.6c):");
                double Muv = Vh * (input.ColD / 2.0);
                double MpbStar = Mpr + Muv;
                double Mpc = input.ColZx * input.ColFy;
                double ratio = Mpc / MpbStar;

                p.Add($"    M_pr = {Mpr:F0} kip-in");
                p.Add($"    M_uv = V_h * (d_c/2) = {Vh:F1} * {input.ColD / 2:F2} = {Muv:F0} kip-in");
                p.Add($"    M_pb* = M_pr + M_uv = {MpbStar:F0} kip-in");
                p.Add($"    M_pc = Zx_c * Fy_c = {input.ColZx:F1} * {input.ColFy} = {Mpc:F0} kip-in");
                p.Add($"    Ratio M_pc / M_pb* = {ratio:F3}");

                colBeamPassed = ratio >= 1.0;
                if (!colBeamPassed)
                    p.Add($"    => FAIL (ratio < 1.0) - Increase column size");
                else
                    p.Add($"    => OK");
                p.Add("");
                p.Add($"    Note: Simplified check (no axial load, one-sided frame).");
                p.Add($"    For final design, include axial load per AISC 341 E3.6c.");
                result.ColumnBeamRatio = ratio;
            }
            else
            {
                p.Add("  IMF: Column-beam relationship per AISC 341 seismic provisions");
                p.Add("  (Strong-column/weak-beam ratio may not be required for IMF)");
                result.ColumnBeamRatio = double.NaN;
            }
            p.Add("");
            result.ColumnBeamPassed = colBeamPassed;

            // ---- Step 5: Beam Shear Strength ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 5: BEAM SHEAR STRENGTH CHECK");
            p.Add("--------------------------------------------------------------------------------");

            double Vn = 0.6 * input.BeamFy * input.BeamD * input.BeamTw;
            double phiVn = PHI_V * Vn;

            p.Add($"  V_u = {Vu:F1} kips");
            p.Add($"  V_n = 0.6*Fy*d*tw = 0.6*{input.BeamFy}*{input.BeamD:F2}*{input.BeamTw:F3} = {Vn:F1} kips");
            p.Add($"  phi*V_n = {PHI_V}*{Vn:F1} = {phiVn:F1} kips");

            bool beamShearPassed = Vu <= phiVn;
            double shearUtil = phiVn > 0 ? Vu / phiVn : 999.0;
            p.Add($"  {(beamShearPassed ? "OK" : "FAIL")} (Utilization: {shearUtil:F3})");
            p.Add("");
            result.BeamShearPassed = beamShearPassed;
            result.ShearRatio = shearUtil;

            // ---- Step 6: Continuity Plates ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 6: CONTINUITY PLATE REQUIREMENTS (SECTION 2.4.4)");
            p.Add("--------------------------------------------------------------------------------");

            double tcfReq = Math.Sqrt(Ff / (0.9 * 6.25 * input.ColFy));

            p.Add($"  Flange force: F_f = {Ff:F1} kips");
            p.Add($"  Column flange t_cf = {input.ColTf:F3} in");
            p.Add("");
            p.Add($"  Flange local bending check (AISC 360 J10.1):");
            p.Add($"    t_cf >= sqrt(F_f/(0.9*6.25*Fyc)) = sqrt({Ff:F1}/({0.9 * 6.25 * input.ColFy:F1})) = {tcfReq:F3} in");

            bool needPlates = input.ColTf < tcfReq;
            if (needPlates)
            {
                double tsMin = Math.Max(input.ColTw, 0.5 * input.BeamTf);
                p.Add($"    => Continuity plates RECOMMENDED (ts >= {tsMin:F3} in)");
            }
            else
            {
                p.Add($"    => Column flange appears adequate (simplified check)");
            }
            result.ContinuityPlatesPassed = !needPlates;
            p.Add("");

            // ---- Step 7: Panel Zone ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 7: COLUMN PANEL ZONE CHECK (SECTION 8.4)");
            p.Add("--------------------------------------------------------------------------------");

            double Vpz = Ff; // One-sided frame
            double VnBasic = 0.6 * input.ColFy * input.ColD * input.ColTw;
            double contribution = 3.0 * input.ColBf * input.ColTf * input.ColTf
                / (input.BeamD * input.ColD * input.ColTw);
            double VnFull = VnBasic * (1.0 + contribution);

            p.Add($"  Panel zone demand: V_pz = F_f = {Vpz:F1} kips (Assuming exterior/one-sided connection)");
            p.Add($"  phi = 1.0 (per AISC 341)");
            p.Add($"  Capacity (basic): Vn = 0.6*Fyc*dc*twc");
            p.Add($"  Vn = 0.6*{input.ColFy}*{input.ColD:F2}*{input.ColTw:F3} = {VnBasic:F1} kips");
            p.Add($"  Capacity (with flange contribution):");
            p.Add($"    3*bcf*tc^2/(db*dc*twc) = 3*{input.ColBf:F2}*{input.ColTf:F3}^2/({input.BeamD:F2}*{input.ColD:F2}*{input.ColTw:F3}) = {contribution:F3}");
            p.Add($"    Vn = {VnBasic:F1} * (1 + {contribution:F3}) = {VnFull:F1} kips");

            bool panelZonePassed = Vpz <= VnFull;
            double pzUtil = VnFull > 0 ? Vpz / VnFull : 999.0;
            if (!panelZonePassed)
                p.Add($"  FAIL - Consider web doubler plates (Utilization: {pzUtil:F3})");
            else
                p.Add($"  OK (Utilization: {pzUtil:F3})");
            result.PanelZonePassed = panelZonePassed;
            result.PanelZoneRatio = pzUtil;
            p.Add("");

            // ---- Step 8: Connection Details ----
            p.Add("--------------------------------------------------------------------------------");
            p.Add("  STEP 8: CONNECTION DETAILING REQUIREMENTS (SECTIONS 8.5-8.6)");
            p.Add("--------------------------------------------------------------------------------");

            double beamDw = input.BeamD - 2 * input.BeamTw;
            double hp = beamDw; // plate height approximately = clear web depth
            double tp = input.BeamTw; // plate thickness >= tw
            double Fyp = input.BeamFy;
            double weldDemand = hp * tp * (0.6 * input.BeamRy * Fyp);
            double filletSize = tp - 1.0 / 16.0;

            p.Add("  BEAM FLANGE-TO-COLUMN (Section 8.5):");
            p.Add("    - CJP groove weld, demand critical per AISC 341");
            p.Add("    - Weld access hole per AWS D1.8 Section 6.11.1.2");
            p.Add("    - Bottom flange backing: remove, backgouge, 5/16\" min reinforcing fillet");
            p.Add("    - Top flange backing: may remain, 5/16\" continuous fillet below CJP");
            p.Add("");
            p.Add("  BEAM WEB-TO-COLUMN (Section 8.6):");
            p.Add("    - CJP groove weld between weld access holes, demand critical");
            p.Add("");
            p.Add("  SINGLE-PLATE SHEAR CONNECTION (Section 8.6(2)):");
            p.Add($"    Plate thickness t_p >= t_w = {input.BeamTw:F3} in");
            p.Add($"    Plate height h_p ~ {hp:F2} in (web depth between flanges)");
            p.Add($"    Plate extends 2 in minimum beyond weld access hole");
            p.Add($"    Weld to column: design shear >= h_p*t_p*(0.6*Ry*Fy)");
            p.Add($"      = {hp:F2}*{tp:F3}*(0.6*{input.BeamRy}*{Fyp})");
            p.Add($"      = {weldDemand:F1} kips");
            p.Add($"    Fillet weld to beam web: size = t_p - 1/16 = {filletSize:F3} in");
            p.Add($"    Fillet weld termination: 1/2 in to 1 in from weld access hole edge");
            p.Add("");
            p.Add("  WELD ACCESS HOLE GEOMETRY (Figure 8.3, AWS D1.8):");
            p.Add("    a = 1/4 in min, 1/2 in max");
            p.Add("    b = 1 in min");
            p.Add("    c = 30 deg (+/- 10 deg)");
            p.Add("    d = 2 in min");
            p.Add("    e = 1/2 in min, 1 in max (fillet weld termination)");
            p.Add("");
            result.ConnectionDetailsPassed = true;

            // ---- Summary ----
            p.Add("================================================================================");
            p.Add("  DESIGN VERIFICATION SUMMARY");
            p.Add("================================================================================");
            p.Add($"  Prequalification:   {(result.PrequalificationPassed ? "PASS" : "FAIL")}");
            p.Add($"  Column-beam:        {(result.ColumnBeamPassed ? "PASS" : "FAIL")}");
            p.Add($"  Beam shear:         {(result.BeamShearPassed ? "PASS" : "FAIL")}");
            p.Add($"  Continuity plates:  {(result.ContinuityPlatesPassed ? "PASS" : "FAIL")}");
            p.Add($"  Panel zone:         {(result.PanelZonePassed ? "PASS" : "FAIL")}");
            p.Add($"  Connection details: {(result.ConnectionDetailsPassed ? "PASS" : "FAIL")}");
            p.Add("");
            p.Add($"  KEY: M_pr={Mpr:F0} kip-in | V_h={Vh:F1} kips | C_pr={CPR_WUFW} | S_h={SH_WUFW}");
            p.Add("");

            result.OverallPassed = result.PrequalificationPassed
                && result.ColumnBeamPassed
                && result.BeamShearPassed
                && result.PanelZonePassed;

            p.Add("================================================================================");
            p.Add(result.OverallPassed
                ? "  ALL CHECKS PASSED"
                : "  SOME CHECKS FAILED - REVIEW AND ADJUST DESIGN");
            p.Add("================================================================================");

            result.IsValid = true;
            return result;
        }

        private static double GetWeight(string name)
        {
            try
            {
                var parts = name.ToUpper().Split('X');
                if (parts.Length > 1 && double.TryParse(parts[1], out double w))
                    return w;
            }
            catch { }
            return 999;
        }
    }
}
