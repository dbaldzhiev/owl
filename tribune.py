import Rhino.Geometry as rg
from dataclasses import dataclass
from typing import List, Tuple, Optional

# ==============================================================================
# 1. SETUP COMPONENTS (DATA STRUCTURES)
# ==============================================================================

@dataclass(frozen=True)
class TribuneSetup:
    """configuration for the tribune base geometry."""
    rows: int
    row_width: float
    elev_counts: List[int]

@dataclass(frozen=True)
class StairSetup:
    """configuration for the stairs/steps."""
    tread_h: float
    tread_w: float

@dataclass(frozen=True)
class RailingSetup:
    """configuration for railings."""
    rail_h: float
    rail_w: float

# Placeholders for future components
class Audience:
    pass

class Screen:
    pass

class Projector:
    pass

class Analysis:
    pass

# ==============================================================================
# 2. SOLVER LOGIC
# ==============================================================================

class TribuneSolver:
    """
    Generates geometry based on setup configurations.
    """
    def __init__(self, 
                 tribune: TribuneSetup, 
                 stairs: StairSetup, 
                 railings: RailingSetup):
        self.tribune = tribune
        self.stairs = stairs
        self.railings = railings

    def solve(self) -> Tuple[Optional[rg.Curve], Optional[rg.Curve], List[rg.Curve], float]:
        """
        Returns:
            tribune_profile (Curve)
            stairs_profile (Curve)
            railings_profile (List[Curve])
            gap (float) - simplified gap calculation
        """
        
        # Validations
        if self.tribune.rows <= 0:
            return None, None, [], 0.0
        
        # -----------------------------
        # A) TRIBUNE PROFILE
        # -----------------------------
        trib_pts = []
        curr_x = 0.0
        curr_z = 0.0
        
        # Start point at origin
        trib_pts.append(rg.Point3d(curr_x, 0, curr_z))
        
        # Row 0 (Ground/Front row base)
        curr_x += self.tribune.row_width
        trib_pts.append(rg.Point3d(curr_x, 0, curr_z))
        
        # Elevated Rows
        railing_curves = []
        
        for r in range(1, self.tribune.rows + 1):
            idx = r - 1
            # Handle list wrapping or clamping for elevation counts
            if self.tribune.elev_counts:
                 if idx < len(self.tribune.elev_counts):
                     count = int(self.tribune.elev_counts[idx])
                 else:
                     count = int(self.tribune.elev_counts[-1])
            else:
                 count = 1
            
            if count < 1: count = 1
            
            row_rise = count * self.stairs.tread_h
            
            # 1. Riser UP
            curr_z += row_rise
            trib_pts.append(rg.Point3d(curr_x, 0, curr_z))
            
            # --- Generate Railing at this Riser ---
            r_bottom_z = curr_z - row_rise
            r_top_z = curr_z + self.railings.rail_h
            
            p0 = rg.Point3d(curr_x, 0, r_bottom_z)
            p1 = rg.Point3d(curr_x, 0, r_top_z)
            p2 = rg.Point3d(curr_x + self.railings.rail_w, 0, r_top_z)
            p3 = rg.Point3d(curr_x + self.railings.rail_w, 0, r_bottom_z)
            
            if self.railings.rail_w < self.tribune.row_width:
                 railing_curves.append(rg.Polyline([p0, p1, p2, p3, p0]).ToNurbsCurve())

            # 2. Tread FORWARD
            curr_x += self.tribune.row_width
            trib_pts.append(rg.Point3d(curr_x, 0, curr_z))

        tribune_crv = rg.Polyline(trib_pts).ToNurbsCurve() if len(trib_pts) > 1 else None

        # -----------------------------
        # B) STAIRS PROFILE
        # -----------------------------
        stair_pts = []
        
        # Stairs start at the beginning of the first elevated row structure.
        # Logic: We calculate the landing positions based on the Tribune logic,
        # then fill the gap backwards with steps.
        
        current_base_x = self.tribune.row_width # Start of Row 1 (first elevated row)
        current_base_z = 0.0
        
        for r in range(self.tribune.rows):
            # Get step count for this rise
            idx = r
            if self.tribune.elev_counts:
                 if idx < len(self.tribune.elev_counts):
                     count = int(self.tribune.elev_counts[idx])
                 else:
                     count = int(self.tribune.elev_counts[-1])
            else:
                 count = 1
                 
            if count < 1: count = 1
            
            rise = self.stairs.tread_h
            run  = self.stairs.tread_w
            
            # The landing for this flight is at:
            target_landing_z = current_base_z + (count * rise)
            # The flight ends at current_base_x (horizontally)
            
            # Calculate where flight starts relative to target
            flight_run = (count - 1) * run
            start_x = current_base_x - flight_run
            
            # If start_x is before the previous landing's end, we have a clash, 
            # but for this solver we assume valid inputs or let it overlap.
            
            # Connect from previous point if exists
            if len(stair_pts) > 0:
                last_pt = stair_pts[-1]
                # If there's a gap between last stair top and new stair start
                if abs(last_pt.X - start_x) > 0.001 or abs(last_pt.Z - current_base_z) > 0.001:
                    stair_pts.append(rg.Point3d(start_x, 0, current_base_z))
            else:
                 # First point
                 stair_pts.append(rg.Point3d(start_x, 0, current_base_z))
            
            # Build steps
            cx, cz = start_x, current_base_z
            for i in range(count):
                cz += rise
                stair_pts.append(rg.Point3d(cx, 0, cz)) # Riser top
                
                if i < count - 1:
                    cx += run
                    stair_pts.append(rg.Point3d(cx, 0, cz)) # Tread end
            
            # Move base to next row
            current_base_x += self.tribune.row_width
            current_base_z = target_landing_z

        stairs_crv = rg.Polyline(stair_pts).ToNurbsCurve() if len(stair_pts) > 1 else None

        # -----------------------------
        # C) CALCULATE GAPS
        # -----------------------------
        # Gap between railing inner face and the stair start of the next row?
        # A simple useful metric: Horizontal distance from railing to stair nosing.
        gap = 0.0 
        
        return tribune_profile, stairs_crv, railing_curves, gap


# ==============================================================================
# 3. COMPONENT INTERFACE (Grasshopper Inputs/Outputs)
# ==============================================================================

# Expecting these variables to be provided by Grasshopper context (or set manually for testing):
# rows, row_width, elev
# tread_h, tread_w
# rail_h, rail_w

def main():
    # Helper to safely get globals or defaults
    def get_global(name, default):
        return globals().get(name, default)

    _rows = get_global('rows', 10)
    _row_width = get_global('row_width', 0.8)
    _elev = get_global('elev', [2])
    
    _tread_h = get_global('tread_h', 0.15)
    _tread_w = get_global('tread_w', 0.28)
    
    _rail_h = get_global('rail_h', 1.0)
    _rail_w = get_global('rail_w', 0.05)

    # 1. SETUP
    t_setup = TribuneSetup(rows=_rows, row_width=_row_width, elev_counts=_elev)
    s_setup = StairSetup(tread_h=_tread_h, tread_w=_tread_w)
    r_setup = RailingSetup(rail_h=_rail_h, rail_w=_rail_w)
    
    # 2. SOLVE
    solver = TribuneSolver(t_setup, s_setup, r_setup)
    trib, stairs, rails, gaps_val = solver.solve()
    
    # 3. OUTPUT
    # Assign to global variables for GH
    # We use explicit global assignment to ensure GH picks them up
    global tribune_setup, stair_setup, rail_setup
    global tribune_profile, stairs_profile, railings_profile, gaps
    
    tribune_setup = t_setup
    stair_setup = s_setup
    rail_setup = r_setup
    
    tribune_profile = trib
    stairs_profile = stairs
    railings_profile = rails
    gaps = gaps_val

if __name__ == "__main__":
    main()
