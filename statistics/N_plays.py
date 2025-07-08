import os
import pandas as pd
import matplotlib.pyplot as plt

# Strategy name mapping
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

# Folder paths
folder_paths = {
    "N0vs0": r"D:\K\Uni\Anul_IV\proj\statistics\data\N_plays\N0vs0",
    "N0vs6": r"D:\K\Uni\Anul_IV\proj\statistics\data\N_plays\N0vs6",
    "N6vs0": r"D:\K\Uni\Anul_IV\proj\statistics\data\N_plays\N6vs0",
    "N6vs6": r"D:\K\Uni\Anul_IV\proj\statistics\data\N_plays\N6vs6",
}

def extract_user_cumulative(df, bot_id):
    df_bot = df[df['user1_id'] == bot_id]  # 🧠 Filter only rows where this bot is user1
    cum = []
    total = 0
    prev = 100
    for _, row in df_bot.iterrows():
        choice = row['choice_type']
        money = row['m_points']

        if choice == 'Buy':
            prev = money - 500  # Buy round: deduct 500 and reset baseline
            continue

        diff = money - prev
        total += diff
        cum.append(total)
        prev = money

    return [0] + cum

def process_nplay_folder(folder_path):
    bot_curves = {}
    for filename in os.listdir(folder_path):
        if not filename.endswith(".csv"):
            continue
        path = os.path.join(folder_path, filename)
        df = pd.read_csv(path)
        if df.empty:
            continue

        bot_id = df['user1_id'].iloc[0]
        strat_name = strat_name_map.get(bot_id, f"Bot {bot_id}")

        curve = extract_user_cumulative(df, bot_id)
        bot_curves[strat_name] = curve
    return bot_curves

# 📈 Plotting loop
for tag, folder in folder_paths.items():
    data = process_nplay_folder(folder)
    plt.figure(figsize=(12, 6))
    for bot, curve in data.items():
        plt.plot(curve, label=bot, linewidth=2)
    plt.title(f"Cumulative Gains for Bots - {tag}")
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
    x1, x2 = -0.2, 140
    y1, y2 = -170, 600
    axins.set_xlim(x1, x2)
    axins.set_ylim(y1, y2)
    axins.set_xticks([])
    axins.set_yticks([])

    # Optional: Draw red lines linking inset and zoom area
    mark_inset(ax, axins, loc1=2, loc2=4, fc="none", ec="black", lw=1)
    plt.tight_layout()
    plt.show()
