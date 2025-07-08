import os
import pandas as pd
import matplotlib.pyplot as plt

strat_name_map = {
    2039: "titfortat",
    2040: "sneaky",
    2041: "random",
    2042: "alwaysdefect",
    2043: "alwayscoop",
    2045: "sample",
    2046: "prober",
    2047: "grudge",
    2048: "tester",
    2051: "titfortat"
}

folder_paths = {
    "6vs6": r"D:\K\Uni\Anul_IV\proj\statistics\data\TomVs\Tom6Vs6",
    "6vs0": r"D:\K\Uni\Anul_IV\proj\statistics\data\TomVs\Tom6Vs0",
    "0vs6": r"D:\K\Uni\Anul_IV\proj\statistics\data\TomVs\Tom0Vs6",
    "0vs0": r"D:\K\Uni\Anul_IV\proj\statistics\data\TomVs\Tom0Vs0",
}

def extract_cumulative(df, user_id):
    cum = []
    total = 0
    prev = 100
    for _, row in df.iterrows():
        if row['pid1'] == user_id:
            choice = row['choice1']
            money = row['money1']
        elif row['pid2'] == user_id:
            choice = row['choice2']
            money = row['money2']
        else:
            continue
        if choice == 'Buy':
            prev = money
            continue
        diff = money - prev
        total += diff
        cum.append(total)
        prev = money
    return [0] + cum

def process_folder(folder_path, folder_tag):
    strategy_curves = {}

    for filename in os.listdir(folder_path):
        if not filename.endswith(".csv"):
            continue
        path = os.path.join(folder_path, filename)
        df = pd.read_csv(path)

        rows = []
        for _, row in df.iterrows():
            pid1, c1, p1 = row['user1_id'], row['choice_type'], row['m_points']
            pid2, c2, p2 = row['user2_id'], row['choice_type2'], row['m_points2']

            is_selfplay = pid1 == 2051 and pid2 == 2039 or pid1 == 2039 and pid2 == 2051
            tom_id = 2051 if folder_tag == "6vs0" and is_selfplay else 2039

            if pid1 == 2051 and pid2 == 2051:
                money1 = p1 - 500 if c1 == 'Buy' else p1
                money2 = p2 - 500 if c2 == 'Buy' else p2
            else:
                money1 = p1 if pid1 != tom_id or c1 != 'Buy' else p1 - 500
                money2 = p2 if pid2 != tom_id or c2 != 'Buy' else p2 - 500

            rows.append({
                'pid1': pid1, 'choice1': c1, 'money1': money1,
                'pid2': pid2, 'choice2': c2, 'money2': money2
            })

        session_df = pd.DataFrame(rows)
        if session_df.empty:
            continue

        # Identify who we are tracking
        if folder_tag == "6vs0":
            pids = set(session_df[['pid1', 'pid2']].values.flatten())
            tom_id = 2051 if 2051 in pids and 2039 in pids else 2039
        else:
            tom_id = 2039

        if tom_id not in session_df[['pid1', 'pid2']].values:
            continue

        first_row = session_df.iloc[0]
        pid1, pid2 = first_row['pid1'], first_row['pid2']
        opp_id = pid2 if pid1 == tom_id else pid1
        label = strat_name_map.get(opp_id, f"User {opp_id}")

        tom_cum = extract_cumulative(session_df, tom_id)

        if label in strategy_curves:
            prev_len = len(strategy_curves[label])
            if len(tom_cum) > prev_len:
                strategy_curves[label] += [strategy_curves[label][-1]] * (len(tom_cum) - prev_len)
            elif len(tom_cum) < prev_len:
                tom_cum += [tom_cum[-1]] * (prev_len - len(tom_cum))
            strategy_curves[label] = [(a + b) / 2 for a, b in zip(strategy_curves[label], tom_cum)]
        else:
            strategy_curves[label] = tom_cum

    return strategy_curves

# Run and plot
for tag, folder in folder_paths.items():
    data = process_folder(folder, tag)
    plt.figure(figsize=(12, 6))
    for strat, curve in data.items():
        plt.plot(curve, label=strat, linewidth=2)
    plt.title(f"Tit-for-Tat Cumulative Gains vs Other Strategies ({tag})")
    plt.xlabel("Round")
    plt.ylabel("Cumulative Gain")
    plt.legend()
    plt.grid(True)
    from mpl_toolkits.axes_grid1.inset_locator import inset_axes, mark_inset

    # Add inset axes (inside the loop where each plot is drawn)
    ax = plt.gca()
    axins = inset_axes(ax, width="70%", height="80%", loc="upper left",
                    bbox_to_anchor=(0.15, 0.45, 0.5, 0.5), bbox_transform=ax.transAxes)

    # Plot same curves in the inset
    for strat, curve in data.items():
        axins.plot(curve, linewidth=1)

    # Zoom region - adjust to your case
    x1, x2 = -0.2, 38
    y1, y2 = -107, 70
    axins.set_xlim(x1, x2)
    axins.set_ylim(y1, y2)
    axins.set_xticks([])
    axins.set_yticks([])

    # Optional: Draw red lines linking inset and zoom area
    mark_inset(ax, axins, loc1=2, loc2=4, fc="none", ec="black", lw=1)
    plt.tight_layout()
    plt.show()