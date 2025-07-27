import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from sklearn.preprocessing import MinMaxScaler
from scipy.stats import f_oneway
import pingouin as pg
import seaborn as sns
import math
import statsmodels.api as sm
import statsmodels.formula.api as smf
from statsmodels.formula.api import mixedlm
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import PolynomialFeatures
from sklearn.metrics import r2_score
import warnings
from sklearn.preprocessing import StandardScaler

warnings.simplefilter('ignore', np.RankWarning)


# === Configuration ===
# Replace these with your actual CSV file paths
snap_files = [#"C://Users//PC//Desktop//StudieDaten//filesP0_snap_NilsZubrot.csv", 
              "C://Users//PC//Desktop//StudieDaten//filesP1_snap_KarimDauer.csv", 
              "C://Users//PC//Desktop//StudieDaten//filesP2_snap_ClemensKnartzt.csv",
              "C://Users//PC//Desktop//StudieDaten//filesP0_erf_snap.csv"
              ]
full_body_files = ["C://Users//PC//Desktop//StudieDaten//filesP3_rot_LukasSeebass.csv",
                   "C://Users//PC//Desktop//StudieDaten//filesP4_rot_JoshuaJung.csv",
                   "C://Users//PC//Desktop//StudieDaten//filesP5_rot_AlisaBiernat.csv"
                   ]

data = [
    {
        "Participant": "P0",
        "Zeitstempel": "2025/06/03 4:03:39 PM OESZ",
        "I remembered the positions of the targets using distinctive features of the environment": 2,
        "I tried to estimate the distances between the targets. ": 5,
        "I relied on my body orientation in the room to locate targets. ": 4,
        "I used my memory of the initial orientation to judge where targets were located after rotation. ": 4,
        "What was your strategy and what cues did you use to keep track of the targets? ": "- tried to point my hand at the targets to remember their positon - i looked at the mountains (am i in between the to mountains or what part of a mountain was in front of a target)",
    },
    {
        "Participant": "P1",
        "Zeitstempel": "2025/06/03 8:56:39 PM OESZ",
        "I remembered the positions of the targets using distinctive features of the environment": 5,
        "I tried to estimate the distances between the targets. ": 5,
        "I relied on my body orientation in the room to locate targets. ": 5,
        "I used my memory of the initial orientation to judge where targets were located after rotation. ": 5,
        "What was your strategy and what cues did you use to keep track of the targets? ": "i kept my arms out in the initial position, so i could come back to my muscle memory and how the arm/ shoulder position felt.",
    },
    {
        "Participant": "P2",
        "Zeitstempel": "2025/06/05 3:16:12 PM OESZ",
        "I remembered the positions of the targets using distinctive features of the environment": 2,
        "I tried to estimate the distances between the targets. ": 5,
        "I relied on my body orientation in the room to locate targets. ": 3,
        "I used my memory of the initial orientation to judge where targets were located after rotation. ": 3,
        "What was your strategy and what cues did you use to keep track of the targets? ": "Ich habe versucht mir Standort vom grünen und blauen Objekt zu merken, wo sind sie im Raum und wie stehen sie zueinander. hat nicht6 immer funktioniert",
    },
    {
        "Participant": "P3",
        "Zeitstempel": "2025/06/11 8:40:45 PM OESZ",
        "I remembered the positions of the targets using distinctive features of the environment": 5,
        "I tried to estimate the distances between the targets. ": 5,
        "I relied on my body orientation in the room to locate targets. ": 5,
        "I used my memory of the initial orientation to judge where targets were located after rotation. ": 5,
        "What was your strategy and what cues did you use to keep track of the targets? ": "I tried to use the rotation of my feet as an indication",
    },
    {
        "Participant": "P4",
        "Zeitstempel": "2025/06/11 9:35:53 PM OESZ",
        "I remembered the positions of the targets using distinctive features of the environment": 4,
        "I tried to estimate the distances between the targets. ": 5,
        "I relied on my body orientation in the room to locate targets. ": 3,
        "I used my memory of the initial orientation to judge where targets were located after rotation. ": 4,
        "What was your strategy and what cues did you use to keep track of the targets? ": "Zuerst habe ich versucht mithilfe der Controller, die ich ja in der virtuellen welt sehe, die Punkte zu markieren indem ich die Hände zu den Objekten hebe. Das hat beim Drehen nicht gut funktioniert. Dann habe ich versucht anhand der Objekte (Berge, Sonne) mir räumliche Linie zu en Objekten vorzustellen. In den letzten Runden Runden habe ich mit meinen Händen immer wieder versucht die Positionen der Gegenstände zu wiederholen und auf diese zu zeigen, wie beim Auswendiglernen.",
    },
    {
        "Participant": "P5",
        "Zeitstempel": "2025/06/23 10:01:00 PM OESZ",
        "I remembered the positions of the targets using distinctive features of the environment": 1,
        "I tried to estimate the distances between the targets. ": 5,
        "I relied on my body orientation in the room to locate targets. ": 5,
        "I used my memory of the initial orientation to judge where targets were located after rotation. ": 1,
        "What was your strategy and what cues did you use to keep track of the targets? ": "measure distance between the targets and always relied to my body orientation",
    }
]

questionnaire_df = pd.DataFrame(data)





# === Utility Functions ===

# accuracy is measured as the Euclidean distance between the participant's 
# response and the correct target location, using the X and Z coordinates.

def compute_accuracy(row):
    return np.sqrt((row['ResponsePos_X'] - row['AnsPos_X'])**2 + (row['ResponsePos_Z'] - row['AnsPos_Z'])**2)

def compute_reaction_time(row):
    return row['EndTime'] - row['BeginTime']


def process_files(file_list, name, onlyfirst=False):
    print(f"\n--- Reading and processing {name} files ---")
    df_all = pd.concat([pd.read_csv(f) for f in file_list])

    # Filter out practice trials
    df_filtered = df_all[df_all['isPractice'] == False].copy() # Use .copy() to prevent SettingWithCopyWarning

    # Filter out decoy targets: keep rows where 'AnsName' does NOT contain 'Decoy'
    df_filtered = df_filtered[~df_filtered['AnsName'].str.contains('Decoy', na=False)].copy()

    # Keep only the first target (blue or green) per trial pair
    def is_first_of_pair(group, **kwargs):
        return group.nsmallest(1, 'trialID')
    
    # Keep only the first of each (assuming each pair has a unique Participant + trialID)
    if (onlyfirst) : df_filtered = df_filtered.groupby(['Participant', 'trialID'], group_keys=False).apply(is_first_of_pair).reset_index(drop=True)    


    #print(f"\n--- Data after filtering for first non-decoy target for {name} (all selected rows) ---")
    #print(df_filtered[['Participant', 'trialID', 'AnsName', 'ResponsePos_X', 'ResponsePos_Z', 'AnsPos_X', 'AnsPos_Z']].to_string())

    # Calculate Accuracy and ReactionTime on the *filtered* data
    df_filtered['Accuracy'] = df_filtered.apply(compute_accuracy, axis=1)
    df_filtered['ReactionTime'] = df_filtered.apply(compute_reaction_time, axis=1)
    
    # --- Outlier Removal by Condition: Mean ± 2.5 × SD for RT and Accuracy ---
    def remove_outliers(group, **kwargs):
        rt_mean, rt_std = group['ReactionTime'].mean(), group['ReactionTime'].std()
        acc_mean, acc_std = group['Accuracy'].mean(), group['Accuracy'].std()

        mask = (
            (group['ReactionTime'] >= rt_mean - 2.5 * rt_std) & (group['ReactionTime'] <= rt_mean + 2.5 * rt_std) &
            (group['Accuracy'] >= acc_mean - 2.5 * acc_std) & (group['Accuracy'] <= acc_mean + 2.5 * acc_std)
        )

        removed_count = len(group) - mask.sum()
        print(f"Condition '{group.name}': removed {removed_count} outlier(s) out of {len(group)}")

        return group[mask]

    before_count = len(df_filtered)
    df_filtered = df_filtered.groupby('Condition', group_keys=False).apply(remove_outliers)
    after_count = len(df_filtered)

    print(f"\n--- Total outliers removed: {before_count - after_count} (from {before_count} → {after_count}) ---")


    print(f"\n--- Calculated Accuracy and Reaction Time for {name} (all first non-decoy target rows) ---")
    print(df_filtered[['Participant', 'trialID', 'AnsName', 'Accuracy', 'ReactionTime', 'Condition']].to_string())

    return df_filtered
    

    
# === Utility ===
def rotate_point(x, z, degrees):
    radians = math.radians(degrees)
    cos_theta = math.cos(radians)
    sin_theta = math.sin(radians)
    x_rot = cos_theta * x - sin_theta * z
    z_rot = sin_theta * x + cos_theta * z
    return x_rot, z_rot

def get_all_rotated_targets(targets, is_rotate_condition):
    if not is_rotate_condition:
        return targets
    angles = [-120, 0, 120]
    return [rotate_point(x, z, angle) for x, z in targets for angle in angles]


def find_nearest_target_to_answer(x, z, target_list):
    return min(target_list, key=lambda t: (t[0] - x) ** 2 + (t[1] - z) ** 2)

# === Plotting ===
def plot_environment(df, layout, title, xlim=None, ylim=None,
                     sample_fraction=1, layout_targets_dict=None,
                     is_rotate_condition=False, isFullRot=False):
    fig, ax = plt.subplots(figsize=(7, 7))
    ax.set_title(title)
    ax.set_xlabel("X Position")
    ax.set_ylabel("Z Position")
    ax.set_aspect('equal', adjustable='box')
    ax.grid(True, linestyle='--', alpha=0.7)

    if xlim:
        ax.set_xlim(xlim)
    else:
        all_x = df['ResponsePos_X']
        ax.set_xlim(all_x.min() - 1, all_x.max() + 1)

    if ylim:
        ax.set_ylim(ylim)
    else:
        all_z = df['ResponsePos_Z']
        ax.set_ylim(all_z.min() - 1, all_z.max() + 1)

    df_layout = df[df["LayoutType"] == layout].copy()
    if sample_fraction < 1.0:
        df_layout = df_layout.sample(frac=sample_fraction, random_state=42) #for reproducability

    # Get and transform layout targets
    base_targets = layout_targets_dict[layout]
    rotations = [-120, 0, 120] if is_rotate_condition or isFullRot else [0]
    transformed_targets = []
    target_colors = ['blue', 'green']

    for angle in rotations:
        for i, (tx, tz) in enumerate(base_targets):
            rx, rz = rotate_point(tx, tz, angle)
            transformed_targets.append(((rx, rz), target_colors[i]))

            # Plot each target with specific color and shape (circle)
            ax.plot(rx, rz, 'o', color=target_colors[i], markersize=10,
                    alpha=0.6, label=f'Target {i+1}' if angle == 0 else "")

    # Strip coordinates for connection calculations
    target_coords = [pt for pt, _ in transformed_targets]

    # Plot responses and lines to nearest target
    plotted_response_legend = False

    for _, row in df_layout.iterrows():
        resp_x, resp_z = row['ResponsePos_X'], row['ResponsePos_Z']
        ans_x, ans_z = row['AnsPos_X'], row['AnsPos_Z']

        # 🔄 NEW: Find target nearest to the correct answer, not response
        nearest_target = find_nearest_target_to_answer(ans_x, ans_z, target_coords)

        # Plot response (always red cross)
        label = 'Response' if not plotted_response_legend else ""
        ax.plot(resp_x, resp_z, marker='x', color='red', markersize=6,
                alpha=0.8, label=label)
        plotted_response_legend = True

        # Draw line from response to the nearest target (to correct position)
        ax.plot([nearest_target[0], resp_x], [nearest_target[1], resp_z],
                color='gray', linestyle='-', linewidth=0.7, alpha=0.5)


    ax.legend(loc='upper right')
    plt.tight_layout()


# === Helper to Extract Factors ===
def extract_factors(df, rotation_type):
    df = df.copy()
    df['TargetType'] = df['Condition'].apply(lambda x: 'Virtual' if 'virtual' in x.lower() else 'Physical')
    df['SelfRotation'] = df['Condition'].apply(lambda x: 'Rotate' if 'rotate' in x.lower() else 'Static')
    df['RotationType'] = rotation_type
    return df

# === Main Plot Driver ===
def plot_all_conditions(dataframe, condition_pairs, title_prefix, layout_targets_dict):
    if title_prefix=='Full Body Rotation:' : 
        isFullRot=True
    else : 
        isFullRot=False
    
    for condition1, condition2, is_rotate in condition_pairs:
        if is_rotate: 
            rotation="with Self Rotation"
        else:
            rotation="Static"
        combined_df = pd.concat([
            dataframe[dataframe['Condition'] == condition1],
            dataframe[dataframe['Condition'] == condition2]
        ])
        for layout in layout_targets_dict.keys():
            plot_environment(
                combined_df,
                layout,
                f' {title_prefix} Layout {layout} {rotation}',
                xlim=(-7, 7),
                ylim=(-7, 7),
                layout_targets_dict=layout_targets_dict,
                is_rotate_condition=is_rotate,
                isFullRot=isFullRot
                
            )

# === Layout Targets Dict ===
layout_targets = {
    'A': [(-0.87, 1.5), (-0.67, 2.5)],
    'B': [(-0.2, 2.5), (0.8, 2.5)],
    'C': [(0.3, 1.5), (-0.7, 1.5)],
    'D': [(0.67, 2.5), (1.5, 1.5)],
}


# SNAP ROTATION
snap_df = process_files(snap_files, "Snap Rotation", True)
snap_df_extracted = extract_factors(snap_df, "Snap")
# FULL BODY ROTATION
full_df = process_files(full_body_files, "Full Body Rotation", True)
full_df_extracted = extract_factors(full_df, "FullBody")

# Combine both datasets
all_df = pd.concat([snap_df_extracted, full_df_extracted])

# === Aggregate Accuracy and Reaction Time per Participant ===
participant_stats = (
    all_df
    .groupby(['Participant', 'RotationType'])[['Accuracy', 'ReactionTime']]
    .agg(['mean', 'std'])
    .reset_index()
)

# Flatten the MultiIndex in columns
participant_stats.columns = ['Participant', 'RotationType', 'Accuracy_Mean', 'Accuracy_Std', 'RT_Mean', 'RT_Std']

# Preview
print("\n=== Participant-level Summary ===")
print(participant_stats)



#Snap
plot_all_conditions(
    dataframe=snap_df,
    condition_pairs=[
        ('physicalRotate', 'virtualRotate', True),
        ('physicalStatic', 'virtualStatic', False)
    ],
    title_prefix='Snap Rotation:',
    layout_targets_dict=layout_targets
)



# FULL BODY
full_df = process_files(full_body_files, "Full Body Rotation", True)
full_df = extract_factors(full_df, "FullBody")
plot_all_conditions(
    dataframe=full_df,
    condition_pairs=[
        ('physicalRotate', 'virtualRotate', True),
        ('physicalStatic', 'virtualStatic', False)
    ],
    title_prefix='Full Body Rotation:',
    layout_targets_dict=layout_targets
)

# Set the style of second analysis
sns.set(style="whitegrid")

def plot_interaction(df, dv, title):
    g = sns.catplot(
        data=df,
        x='SelfRotation', y=dv,
        hue='TargetType',
        col='RotationType',
        kind='point',
        dodge=True,
        capsize=.1,
        err_kws={'linewidth': 1},
        palette='Set2',
        height=5,
        aspect=1
    )
    g.fig.subplots_adjust(top=0.85)
    g.fig.suptitle(title)
    g.set_axis_labels("Self Rotation", dv + (" (Euclidean Distance)" 
                                             if title == "Accuracy by RotationType" 
                                             else " in s"))
    plt.show()
    
    
# === Accuracy Interaction Plot ===
plot_interaction(all_df, 'Accuracy', "Accuracy by RotationType")

# === Reaction Time Interaction Plot ===
plot_interaction(all_df, 'ReactionTime', "Reaction Time by RotationType")


# === Plot: Accuracy per Participant ===
plt.figure(figsize=(10, 6))
sns.barplot(
    data=participant_stats,
    x='Participant',
    y='Accuracy_Mean',
    hue='RotationType',
    palette='Set2',
    ci=None
)
# Add error bars manually
for i, row in participant_stats.iterrows():
    plt.errorbar(
        x=i, y=row['Accuracy_Mean'], yerr=row['Accuracy_Std'],
        fmt='none', c='black', capsize=5
    )

plt.title("Mean Accuracy per Participant (with Std Dev)")
plt.ylabel("Mean Euclidean Distance")
plt.xticks(rotation=45)
plt.tight_layout()
plt.show()


# === Plot: Reaction Time per Participant ===
plt.figure(figsize=(10, 6))
sns.barplot(
    data=participant_stats,
    x='Participant',
    y='RT_Mean',
    hue='RotationType',
    palette='Set2',
    ci=None
)
# Add error bars manually
for i, row in participant_stats.iterrows():
    plt.errorbar(
        x=i, y=row['RT_Mean'], yerr=row['RT_Std'],
        fmt='none', c='black', capsize=5
    )

plt.title("Mean Reaction Time per Participant (with Std Dev)")
plt.ylabel("Mean Reaction Time (s)")
plt.xticks(rotation=45)
plt.tight_layout()
plt.show()



plt.show()


# Mixed Linear Model Regression Results 
#============== ACCURACY ============
all_df['TargetType'] = all_df['TargetType'].astype('category')
all_df['SelfRotation'] = all_df['SelfRotation'].astype('category')
all_df['RotationType'] = all_df['RotationType'].astype('category')

# Use statsmodels MixedLM for full mixed ANOVA
model = mixedlm("Accuracy ~ TargetType * SelfRotation * RotationType",
                data=all_df,
                groups=all_df["Participant"],
                re_formula="~1")

result = model.fit()
print(result.summary())



# Make sure your data is cleaned and ready (e.g., no NaNs in ReactionTime)
# model formula includes all main effects and interactions
model_rt = mixedlm("ReactionTime ~ TargetType * SelfRotation * RotationType",
    data=all_df,
    groups="Participant",
    re_formula="~1")
result_rt = model_rt.fit()
print(result_rt.summary())



# STEP 1: Calculate Presence Score
# Assuming 'questionnaire_df' is already loaded
questionnaire_numeric = questionnaire_df.select_dtypes(include=[np.number])
questionnaire_df['Presence_Score'] = questionnaire_numeric.mean(axis=1)

# Debug: View columns to verify presence of 'Participant'
print(questionnaire_df.columns.tolist())

# STEP 2: Merge with Performance Summary
merged_data = pd.merge(
    participant_stats,
    questionnaire_df[['Participant', 'Presence_Score']],
    on='Participant'
)

# Plot setup
sns.set(style="whitegrid")
plt.figure(figsize=(10, 6))

colors = {'Snap': 'blue', 'FullBody': 'red'}
rotation_types = merged_data['RotationType'].unique()

for rotation in rotation_types:
    subset = merged_data[merged_data['RotationType'] == rotation]

    # Prepare data
    X = subset[['Presence_Score']].values
    y = subset['Accuracy_Mean'].values
    y_err = subset['Accuracy_Std'].values

    # Sort values for consistent plotting
    sort_idx = np.argsort(X.flatten())
    X_sorted = X[sort_idx]
    y_sorted = y[sort_idx]
    y_err_sorted = y_err[sort_idx]

    # Quadratic model
    poly = PolynomialFeatures(degree=2)
    X_poly = poly.fit_transform(X_sorted)
    model = LinearRegression().fit(X_poly, y_sorted)
    y_pred = model.predict(X_poly)


# Plot: Accuracy vs. Presence, separated by Rotation Type, with quadratic fit
lm= sns.lmplot(
    data=merged_data,
    x='Presence_Score',
    y='Accuracy_Mean',
    hue='RotationType',
    order=2,  # Quadratic fit
    height=6,
    aspect=1.2,
    markers=['o', 's'],
    palette='Set1',

)

# Access the legend object
legend = lm._legend  # This is the Legend object in the FacetGrid

# Update legend properties
legend.set_title("Rotation Type")
for text in legend.texts:
    text.set_fontsize(10)
legend.get_title().set_fontsize(11)

legend.set_bbox_to_anchor((1, 0.74))  # x=center, y=below
legend.set_loc("lower center")           # anchor point
legend._legend_box.align = "left"        # Align legend entries horizontally

# Update plot labels and title
lm.set_axis_labels("Sense of Presence (Mean Likert Scale)", "Accuracy (Mean Euclidean Distance)", fontsize=12)
lm.fig.suptitle("Accuracy vs. Sense of Presence by Rotation Type with Regression", fontsize=14, weight='bold')
lm.set_titles("")  # Remove default FacetGrid titles if any

# Adjust layout
plt.tight_layout()
plt.grid(True)
plt.subplots_adjust(top=0.92)  # To make space for suptitle
plt.show()



plt.figure(figsize=(8, 6))
sns.set(style="whitegrid")




sns.lmplot(
    data=merged_data,
    x='Presence_Score',
    y='RT_Mean',
    hue='RotationType',
    order=2,  # <--- quadratic
    height=6,
    aspect=1.2,
    markers=['o', 's'],
    palette='Set1'
)

plt.title("Reaction Time vs. Sense of Presence by Rotation Type", fontsize=14, weight='bold')
plt.xlabel("Sense of Presence (Avg Questionnaire Score)", fontsize=12)
plt.ylabel("Reaction Time (Mean, s)", fontsize=12)
plt.tight_layout()
plt.show()



X = merged_data[['Presence_Score']].values
y = merged_data['RT_Mean'].values

# Linear model
lin_reg = LinearRegression().fit(X, y)
y_pred_linear = lin_reg.predict(X)

# Quadratic model

poly = PolynomialFeatures(degree=2)
X_poly = poly.fit_transform(X)
quad_reg = LinearRegression().fit(X_poly, y)
y_pred_quad = quad_reg.predict(X_poly)

# R² scores
r2_linear = r2_score(y, y_pred_linear)
r2_quad = r2_score(y, y_pred_quad)

scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

# Then proceed with poly transform and regression
X_poly = poly.fit_transform(X_scaled)

print(f"R² Linear: {r2_linear:.3f}")
print(f"R² Quadratic: {r2_quad:.3f}")


print("Unique Presence Scores:", np.unique(X).size)



print("\n=== Accuracy ===")
# Store coefficients
coeffs_by_rotation = {}

# Loop over each rotation type
for rotation in merged_data['RotationType'].unique():
    subset = merged_data[merged_data['RotationType'] == rotation]

    X = subset[['Presence_Score']].values
    y = subset['Accuracy_Mean'].values

    # Generate quadratic features
    poly = PolynomialFeatures(degree=2)
    X_poly = poly.fit_transform(X)

    # Fit regression model
    model = LinearRegression().fit(X_poly, y)

    # Extract coefficients
    intercept = model.intercept_
    coef_linear = model.coef_[1]
    coef_quadratic = model.coef_[2]

    coeffs_by_rotation[rotation] = {
        'intercept': intercept,
        'linear_coef': coef_linear,
        'quadratic_coef': coef_quadratic,
        'r_squared': model.score(X_poly, y)
    }

# Print results
for rotation, coeffs in coeffs_by_rotation.items():
    print(f"\n=== {rotation} Rotation ===")
    print(f"Intercept        : {coeffs['intercept']:.4f}")
    print(f"Linear Coef      : {coeffs['linear_coef']:.4f}")
    print(f"Quadratic Coef   : {coeffs['quadratic_coef']:.4f}")
    print(f"R² Score         : {coeffs['r_squared']:.4f}")
    
print("\n=== Reaction Time ===")
# Store coefficients
coeffs_by_rotation = {}

# Loop over each rotation type
for rotation in merged_data['RotationType'].unique():
    subset = merged_data[merged_data['RotationType'] == rotation]

    X = subset[['Presence_Score']].values
    y = subset['RT_Mean'].values

    # Generate quadratic features
    poly = PolynomialFeatures(degree=2)
    X_poly = poly.fit_transform(X)

    # Fit regression model
    model = LinearRegression().fit(X_poly, y)

    # Extract coefficients
    intercept = model.intercept_
    coef_linear = model.coef_[1]
    coef_quadratic = model.coef_[2]

    coeffs_by_rotation[rotation] = {
        'intercept': intercept,
        'linear_coef': coef_linear,
        'quadratic_coef': coef_quadratic,
        'r_squared': model.score(X_poly, y)
    }

# Print results
for rotation, coeffs in coeffs_by_rotation.items():
    print(f"\n=== {rotation} Rotation ===")
    print(f"Intercept        : {coeffs['intercept']:.4f}")
    print(f"Linear Coef      : {coeffs['linear_coef']:.4f}")
    print(f"Quadratic Coef   : {coeffs['quadratic_coef']:.4f}")
    print(f"R² Score         : {coeffs['r_squared']:.4f}")
    
    
# Accuracy Model with Interaction
model_acc = smf.ols('Accuracy_Mean ~ Presence_Score * RotationType', data=merged_data).fit()
anova_acc = sm.stats.anova_lm(model_acc, typ=2)

# Reaction Time Model with Interaction
model_rt = smf.ols('RT_Mean ~ Presence_Score * RotationType', data=merged_data).fit()
anova_rt = sm.stats.anova_lm(model_rt, typ=2)

# Print ANOVA tables
print("=== ANOVA: Accuracy ===")
print(anova_acc)

print("\n=== ANOVA: Reaction Time ===")
print(anova_rt)

# Optionally: Show model summaries
print("\n--- Accuracy Model Summary ---")
print(model_acc.summary())

print("\n--- Reaction Time Model Summary ---")
print(model_rt.summary())


