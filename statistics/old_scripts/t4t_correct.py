import os
import pandas as pd
import matplotlib.pyplot as plt

bot_name_map = {
    2039: "Tom",
    2040: "Steve",
    2041: "Roy",
    2042: "Dave",
    2043: "Carl",
    2045: "Sam",
    2046: "Peter",
    2047: "Fred",
    2048: "Trevor",
    2051: "Tom"
}

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

def plot_adjusted_cumulative_gains(folder_path, tom_id=2039):
    for filename in os.listdir(folder_path):
        if not filename.endswith(".csv"):
            continue

        path = os.path.join(folder_path, filename)
        df = pd.read_csv(path)

        rows = []

        for _, row in df.iterrows():
            pid1, c1, p1 = row['user1_id'], row['choice_type'], row['m_points']
            pid2, c2, p2 = row['user2_id'], row['choice_type2'], row['m_points2']

            if pid1 == 2051 and pid2 == 2051:
                # Self-play dummy; handle buy only once
                money = p1 - 500 if c1 == 'Buy' else p1
                rows.append({
                    'pid1': pid1, 'choice1': c1, 'money1': money,
                    'pid2': pid2, 'choice2': c2, 'money2': money  # duplicate same money
                })
                continue

            is_duplicate_buy = (
                pid1 == pid2 and c1 == 'Buy' and c2 == 'Buy'
            )

            money1 = p1 - 500 if c1 == 'Buy' else p1
            money2 = p2 - 500 if c2 == 'Buy' else p2
            if is_duplicate_buy:
                money1 = p1 - 500  # subtract from only one side

            rows.append({
                'pid1': pid1, 'choice1': c1, 'money1': money1,
                'pid2': pid2, 'choice2': c2, 'money2': money2
            })

        session_df = pd.DataFrame(rows)

        def extract_cumulative(df, user_id):
            cum = []
            total = 0
            prev = 100

            for i, row in df.iterrows():
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

        # Determine who Tom is and who opponent is
        if session_df['pid1'].iloc[0] == tom_id:
            opp_id = session_df['pid2'].iloc[0]
            tom_cum = extract_cumulative(session_df, tom_id)
            opp_cum = extract_cumulative(session_df, opp_id)
        elif session_df['pid2'].iloc[0] == tom_id:
            opp_id = session_df['pid1'].iloc[0]
            tom_cum = extract_cumulative(session_df, tom_id)
            opp_cum = extract_cumulative(session_df, opp_id)
        else:
            continue  # Skip file if Tom not present

        # Align both lines
        max_len = max(len(tom_cum), len(opp_cum))
        tom_cum += [tom_cum[-1]] * (max_len - len(tom_cum))
        opp_cum += [opp_cum[-1]] * (max_len - len(opp_cum))

        filename_base = os.path.splitext(filename)[0]  # Strip ".csv"
        bot0_tag, bot6_tag = filename_base.split("Vs")

        def resolve_label(pid, side_tag):
            base_strat = strat_name_map.get(pid, f"User {pid}")
            label = "(without buy)" if side_tag.endswith("0") else "(with buy)"
            return f"{base_strat} {label}"

        if tom_id == session_df['pid1'].iloc[0]:
            tom_strat = resolve_label(tom_id, bot0_tag)
            opp_strat = resolve_label(opp_id, bot6_tag)
        else:
            tom_strat = resolve_label(tom_id, bot6_tag)
            opp_strat = resolve_label(opp_id, bot0_tag)

        plt.figure(figsize=(10, 5))
        plt.plot(range(max_len), tom_cum, label=tom_strat, linewidth=2)
        plt.plot(range(max_len), opp_cum, label=opp_strat, linewidth=2)
        plt.title(f"Cumulative Gains (Buys Ignored) – {filename}")
        plt.xlabel("Round")
        plt.ylabel("Cumulative Gain")
        plt.legend()
        plt.grid(True)
        plt.tight_layout()
        plt.show()

        #debug_cumulative_gains(session_df, tom_id, label="Tom")
        #debug_cumulative_gains(session_df, opp_id, label="Opponent")

def debug_cumulative_gains(df, user_id, label="User"):
    print(f"\n🔍 Step-by-step gains for '{label}' (User ID: {user_id})")
    
    prev = 100
    total = 0

    for i, row in df.iterrows():
        pid1, c1, m1 = row['pid1'], row['choice1'], row['money1']
        pid2, c2, m2 = row['pid2'], row['choice2'], row['money2']

        if pid1 == user_id:
            choice, money = c1, m1
        elif pid2 == user_id:
            choice, money = c2, m2
        else:
            continue

        if choice == 'Buy':
            print(f"Row {i}: BUY → Reset prev to {money} (ignored in gains)")
            prev = money
            continue

        diff = money - prev
        total += diff
        print(f"Row {i}: Choice={choice}, Money={money}, Prev={prev}, Diff={diff}, Total={total}")
        prev = money

    print(f"\n✅ Final total gain for '{label}': {total}")

# Example usage
folder = r"D:\K\Uni\Anul_IV\proj\statistics\data\TomVs\Tom"
plot_adjusted_cumulative_gains(folder)