import pandas as pd
import os
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

            if pid1 != 2051:
                user_sessions.setdefault(pid1, {}).setdefault(filename, []).append((choice1, points1))
                user_choices.setdefault(pid1, []).append(choice1)

            if pid2 != 2051:
                user_sessions.setdefault(pid2, {}).setdefault(filename, []).append((choice2, points2))
                user_choices.setdefault(pid2, []).append(choice2)

    user_earnings = {}
    for uid, sessions in user_sessions.items():
        total = 0
        for session_rows in sessions.values():
            prev = 100
            for choice, money in session_rows:
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
    plt.title("Total Money Earned per Strategy (Buy Disabled)")
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
    folder = r"D:\K\Uni\Anul_IV\proj\statistics\data\inf_vs_inf"
    result, user_sessions, user_choices = calculate_total_earnings(folder)
    print("\n=== Total Earned Money (Buy Disabled) ===")
    print(result)
    plot_earnings(result)
    #debug_user_earnings(user_sessions, 2042, strat_name_map.get(2042))  # prober

    choice_stats = []
    for uid, choices in user_choices.items():
        coop = choices.count('Coop')
        deflect = choices.count('Deflect')
        buy = choices.count('Buy')
        total = len(choices)
        strat = strat_name_map.get(uid, f"User {uid}")
        choice_stats.append({
            'Strategy': strat,
            'Coop': coop,
            'Deflect': deflect,
            'Buy': buy,
            'Total': total
        })

    df_choices = pd.DataFrame(choice_stats).set_index('Strategy')
    df_choices = df_choices[df_choices['Total'] > 0]  # Remove empty rows

    if df_choices.empty:
        print("⚠️ No valid choice data found to plot.")
    else:
        df_props = df_choices[['Coop', 'Deflect', 'Buy']].div(df_choices['Total'], axis=0).fillna(0)

        print("\n=== Choice Counts by Strategy (Buy Disabled) ===")
        print(df_choices[['Coop', 'Deflect', 'Buy']])

        print("\n=== Choice Proportions by Strategy (Buy Disabled) ===")
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
        plt.title("Choice Distribution by Strategy (Buy Disabled)")
        plt.xlabel("Strategy")
        plt.ylabel("Proportion of Moves")
        plt.xticks(rotation=45)
        plt.legend(title="Choice")
        plt.tight_layout()
        plt.show()

# ✅ Filter out strategies with non-positive earnings
safe_result = result[result['TotalMoneyEarned'] > 0]

# Check if there's anything to plot
if safe_result.empty:
    print("⚠️ No strategies with positive earnings to plot.")
else:
    plt.figure(figsize=(8, 8))
    plt.pie(
        safe_result['TotalMoneyEarned'],
        labels=safe_result['Strategy'],
        autopct='%1.1f%%',
        startangle=140
    )
    plt.title("Market Share by Total Money Earned (Buy Disabled)")
    plt.axis('equal')
    plt.tight_layout()
    plt.show()