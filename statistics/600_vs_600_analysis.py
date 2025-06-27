import pandas as pd
import numpy as np
import os
import matplotlib.pyplot as plt
from collections import defaultdict

# === STRATEGY MAPPING ===
strat_name_map = {
    2039: "titfortat",
    2040: "sneaky",
    2041: "random",
    2042: "alwaysdefect",
    2043: "alwayscoop",
    2045: "sample",
    2046: "prober",
    2047: "grudge",
    2048: "tester"
}

def calculate_total_earnings(folder_path):
    user_sessions = {}
    user_choices = {}

    for filename in os.listdir(folder_path):
        if not filename.endswith(".csv"):
            continue

        path = os.path.join(folder_path, filename)
        df = pd.read_csv(path)

        for _, row in df.iterrows():
            pid1, choice1, points1 = row['user1_id'], row['choice_type'], row['m_points']
            pid2, choice2, points2 = row['user2_id'], row['choice_type2'], row['m_points2']

            if pid1 == 2051 and pid2 == 2051:
                continue  # Skip placeholder

            is_duplicate_buy = (
                pid1 == pid2 and
                choice1 == 'Buy' and
                choice2 == 'Buy'
            )

            # Determine adjusted money only once per player in this row
            money1 = points1
            money2 = points2

            if is_duplicate_buy:
                # Subtract -500 from only one side (e.g., user1)
                money1 -= 500
            else:
                if choice1 == 'Buy':
                    money1 -= 500
                if choice2 == 'Buy':
                    money2 -= 500

            if pid1 != 2051:
                user_sessions.setdefault(pid1, {}).setdefault(filename, []).append((choice1, money1))
                user_choices.setdefault(pid1, [])
                if not is_duplicate_buy or pid2 != pid1:
                    user_choices[pid1].append(choice1)
            if pid2 != 2051:
                user_sessions.setdefault(pid2, {}).setdefault(filename, []).append((choice2, money2))
                user_choices.setdefault(pid2, []).append(choice2)

    # === Compute actual earnings as sum of diffs per session
    user_earnings = {}
    for uid, sessions in user_sessions.items():
        total = 0
        for session_rows in sessions.values():
            prev = 100
            for choice, money in session_rows:
                if choice == 'Buy':
                    prev = money  # Already adjusted
                    continue
                diff = money - prev
                total += diff
                prev = money
        user_earnings[uid] = total

    result_df = pd.DataFrame([
        {
            'UserId': uid,
            'Strategy': strat_name_map.get(uid, f"User {uid}"),
            'TotalMoneyEarned': total
        }
        for uid, total in user_earnings.items()
    ])

    result_df = result_df.sort_values(by='TotalMoneyEarned', ascending=False).reset_index(drop=True)
    return result_df, user_sessions, user_choices

def plot_earnings(result_df):
    plt.figure(figsize=(10, 6))
    bars = plt.bar(result_df['Strategy'], result_df['TotalMoneyEarned'])
    for bar in bars:
        height = bar.get_height()
        plt.text(bar.get_x() + bar.get_width()/2, height + 5, f'{int(height)}', ha='center', va='bottom', fontsize=9)
    plt.xlabel("Strategy")
    plt.ylabel("Total Money Earned")
    plt.title("Total Money Earned per Strategy (Buy Enabled)")
    plt.xticks(rotation=45)
    plt.tight_layout()
    plt.show()

def debug_user_earnings(user_sessions, user_id, strat_name=None):
    if user_id not in user_sessions:
        print(f"\n❌ No data found for user {user_id}")
        return

    label = strat_name or f"User {user_id}"
    print(f"\n🔍 Step-by-step for '{label}' (User ID: {user_id})")
    total = 0

    for session_name, session_rows in user_sessions[user_id].items():
        print(f"\n--- Session: {session_name} ---")
        prev = 100

        for i, (choice, money) in enumerate(session_rows):
            if choice == 'Buy':
                print(f"Row {i}: BUY → Reset prev to {money} (Money after -500 already)")
                prev = money
                continue

            diff = money - prev
            total += diff
            print(f"Row {i}: Choice={choice}, Money={money}, Prev={prev}, Diff={diff}, Total={total}")
            prev = money

    print(f"\n✅ Final total earnings for '{label}': {total}")

if __name__ == "__main__":
    folder = r"D:\K\Uni\Anul_IV\proj\statistics\data\600_vs_600"
    result, user_sessions, user_choices = calculate_total_earnings(folder)
    print("\n=== Total Earned Money (Buy Enabled) ===")
    print(result)
    plot_earnings(result)
    #debug_user_earnings(user_sessions, 2046, strat_name_map.get(2046))  # prober

    choice_stats = []
    for uid, choices in user_choices.items():
        coop = choices.count('Coop')
        deflect = choices.count('Deflect')
        buy = choices.count('Buy')
        total = len(choices)
        strat = strat_name_map.get(uid, f"User {uid}")
        try:
            first_buy_index = choices.index('Buy') + 1  # +1 for 1-based count
        except ValueError:
            first_buy_index = None  # or -1 if you prefer

        choice_stats.append({
            'Strategy': strat,
            'Coop': coop,
            'Deflect': deflect,
            'Buy': buy,
            'Total': total,
            'FirstBuyRound': first_buy_index
        })

    df_choices = pd.DataFrame(choice_stats).set_index('Strategy')
    df_choices = df_choices[df_choices['Total'] > 0]  # Remove empty rows

    if df_choices.empty:
        print("⚠️ No valid choice data found to plot.")
    else:
        df_props = df_choices[['Coop', 'Deflect', 'Buy']].div(df_choices['Total'], axis=0).fillna(0)

        print("\n=== Choice Counts by Strategy (Buy Enabled) ===")
        print(df_choices[['Coop', 'Deflect', 'Buy']])

        print("\n=== Choice Proportions by Strategy (Buy Enabled) ===")
        print(df_props)

        ax = df_props.plot(kind='bar', stacked=True, figsize=(10, 6))

        # Add raw counts from df_choices onto the bars
        for idx, strategy in enumerate(df_choices.index):
            y_offset = 0
            for choice in ['Coop', 'Deflect', 'Buy']:
                count = df_choices.loc[strategy, choice]
                proportion = df_props.loc[strategy, choice]
                if proportion > 0:
                    ax.text(
                        idx,                     # x-position (bar index)
                        y_offset + proportion / 2,  # y-position (middle of the segment)
                        str(count),             # label
                        ha='center', va='center', fontsize=8
                    )
                    y_offset += proportion
        plt.title("Choice Distribution by Strategy (Buy Enabled)")
        plt.xlabel("Strategy")
        plt.ylabel("Proportion of Moves")
        plt.xticks(rotation=45)
        plt.legend(title="Choice")
        plt.tight_layout()
        plt.show()

# === Collect all strategies from strat_name_map ===
all_strats = list(strat_name_map.values())

# === Collect First Buy Rounds Per Session ===
first_buy_rounds = defaultdict(list)

for uid, sessions in user_sessions.items():
    strat = strat_name_map.get(uid, f"User {uid}")
    for session_rows in sessions.values():
        try:
            round_index = next(i for i, (choice, _) in enumerate(session_rows) if choice == 'Buy')
            first_buy_rounds[strat].append(round_index + 1)
        except StopIteration:
            continue  # Did not buy in this session

# === Calculate Average First Buy Round, including 0 for "never bought" ===
avg_first_buy_data = []
for strat in sorted(all_strats):
    rounds = first_buy_rounds.get(strat, [])
    if rounds:
        avg = round(sum(rounds) / len(rounds), 2)
    else:
        avg = 0  # You could use np.nan here if you want to visually mark as "Never"
    avg_first_buy_data.append({'Strategy': strat, 'AvgFirstBuyRound': avg})

df_avg_buy = pd.DataFrame(avg_first_buy_data).set_index('Strategy')
df_avg_buy = df_avg_buy.sort_values(by='AvgFirstBuyRound', ascending=True)

# === Print Table ===
print("\n=== Average First Buy Round per Strategy ===")
print(df_avg_buy)

# === Plot with 'Never' (avg = 0) colored differently ===
plt.figure(figsize=(10, 6))
bars = plt.bar(df_avg_buy.index, df_avg_buy['AvgFirstBuyRound'], color='skyblue')

for i, val in enumerate(df_avg_buy['AvgFirstBuyRound']):
    if val == 0:
        bars[i].set_color('lightcoral')
        plt.text(i, 0.5, "Never", ha='center', va='bottom', fontsize=9, rotation=90)
    else:
        plt.text(i, val + 0.5, f'{val:.1f}', ha='center', va='bottom', fontsize=9)

plt.title("Average Round of First Buy per Strategy")
plt.xlabel("Strategy")
plt.ylabel("Avg. Round of First Buy")
plt.xticks(rotation=45)
plt.grid(True)
plt.tight_layout()
plt.show()

def debug_first_buy_rounds(user_sessions, strat_name_to_debug, strat_name_map):
    print(f"\n🔍 Debugging First Buy Rounds for: {strat_name_to_debug}")
    
    # Find the UID for the given strategy name
    uid_to_debug = None
    for uid, name in strat_name_map.items():
        if name == strat_name_to_debug:
            uid_to_debug = uid
            break

    if uid_to_debug is None:
        print("❌ Strategy not found.")
        return

    sessions = user_sessions.get(uid_to_debug, {})
    if not sessions:
        print("❌ No session data for this user.")
        return

    total = 0
    count = 0

    for session_name, session_rows in sessions.items():
        try:
            index = next(i for i, (choice, _) in enumerate(session_rows) if choice == 'Buy')
            print(f"✅ {session_name}: First buy at round {index + 1}")
            total += (index + 1)
            count += 1
        except StopIteration:
            print(f"❌ {session_name}: Never bought")

    if count > 0:
        avg = round(total / count, 2)
        print(f"\n📊 Average first buy round across {count} sessions: {avg}")
    else:
        print("\n⚠️ Strategy never bought in any session.")

debug_first_buy_rounds(user_sessions, "titfortat", strat_name_map)

# Filter out strategies with negative or zero earnings
positive_result = result[result['TotalMoneyEarned'] > 0]

# Plot pie chart
plt.figure(figsize=(8, 8))
plt.pie(
    positive_result['TotalMoneyEarned'],
    labels=positive_result['Strategy'],
    autopct='%1.1f%%',
    startangle=140
)
plt.title("Market Share by Total Money Earned (Buy Enabled)")
plt.axis('equal')
plt.tight_layout()
plt.show()