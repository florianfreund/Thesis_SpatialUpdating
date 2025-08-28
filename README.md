# Bachelor Thesis by Florian Freund


Abstract:

Spatial updating is the cognitive process that allows individuals keep track of object locations in a space during self-motion or orientation change [Riecke2007Spatial].
Virtual Reality (VR), as a dual-environment paradigm, enables overlapping physical and virtual spaces [liftonDualRealityMerging2010, TsengEtAl].
In a previous study, the gap was investigated how spatial updating performance in dual environments is related to the sense of presence in VR by manipulating self-rotation [TsengEtAl]. 
However, limitations of their study design include the implementation of rotation tasks and that it does not account for diverse rotation techniques, such as Snap Rotation, as a discrete and incremental reorientation via joystick commonly used in VR applications.
To address these limitations I optimized the previous study design by aligning trial orientations (refining the rotation) and introducing a Snap Rotation condition. 
A preliminary study (N=6) was conducted to explore how self-rotation (rotate vs. static), encoded environment (virtual vs. physical) and rotation method (Snap Rotation vs. Full-Body Rotation) affect spatial updating performance in dual environments using VR.
Optimizing the rotation tasks did not reveal statistically significant core findings - likely due to the small sample size - not validating and not failing the previous study. 
The inclusion of Snap Rotation resulted in increasing reaction times and decreasing pointing accuracy, especially during rotate trials.
This refined experimental framework incorporates Snap Rotation and is intended to be reused and adapted by future researchers investigating spatial updating in dual environments. 


# VR Dual Environment Alignment and Data Analysis

This project includes a Unity-based VR experiment developed for the Meta Quest 3 and a companion Python analysis script. The goal is to align virtual and physical environments and collect participant data for behavioral analysis.

---

## Experimental Setup

- **Platform**: Unity 2022.3 (URP)
- **Hardware**: Meta Quest 3 (standalone mode, no PC required)
- **Modes**:
  - **Virtual Condition**
  - **Physical Condition**: Utilizes Meta Quest’s pass-through to display real-world targets
  - **Both:** rotate vs. static

---

## Snap Rotation (In-VR Controls)

- **Right Joystick**:
  - Push **right** → rotate scene 20° to the **right**
  - Push **left** → rotate scene 20° to the **left**
- Designed for comfortable reorientation and reduced motion sickness, increment value can be modified

---

## Dual Environment Alignment in Unity

A Unity C# script (`Alignment.cs`) is provided to align the virtual scene to the physical environment using two real-world reference points (A and B).

### Alignment before starting the experiment

1. **Setup**:
   - Attach the alignment script to a parent GameObject that contains all virtual content.
   - Do **not** include the XR camera or controllers as children of this parent object.
   - Place two virtual reference objects (e.g., a small sphere and a cube) at known positions in the Unity scene.

2. **In-VR Alignment Procedure**:
   - Place the **left controller** at the physical location for **Reference Point A**, press **Button X**.
   - Move the controller to **Reference Point B**, press **Button X** again.
   - The system applies:
     - **Rotation**: Aligns the A→B vector between physical and virtual scenes.
     - **Scaling**: Matches distances between points A and B.
     - **Translation**: Aligns virtual point A to the real point A.
   - Press **Button X** again to **confirm alignment**.
   - Press **Button Y** to **restart alignment** if needed.

3. **After Alignment**:
   - Reference points and passthrough visuals are disabled.
   - A start panel and instructions are shown.
   - Calibration data is logged to a CSV file.

---

## Python Data Analysis

Once the experiment is completed, a Python script (`logAnalysis.py`) can be used to analyze the data collected from participants.

### Requirements

- **Python**: Version 3.12
- **Libraries Used**:
  pandas, matplotlib.pyplot, numpy, sklearn.preprocessing, sklearn.linear_model, sklearn.metrics,
  scipy.stats, pingouin, seaborn, math, statsmodels.api, statsmodels.formula.api


## Optimization Suggestions

- **CSV Output Issue**  
  One row of the output CSV file contained part of the CSV header.  
  Check write operations to prevent errors.

- **Performance Drift**  
  Reduced vigilance was observed during highly monotonous, repetitive work of participants.  
  Verify by comparing the average performance of early trials with later ones.

- **Virtual Environment Cues**  
  Modify the virtual environment by adding more visual cues.  
  Investigate the effects.

> For further details, see the thesis discussion.



