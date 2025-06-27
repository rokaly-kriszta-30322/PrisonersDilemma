import os
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

folder_path = r"D:\K\Uni\Anul_IV\proj\statistics\data\inf_vs_600"

bot_name_map = {
    2039: "Tom",
    2040: "Steve",
    2041: "Roy",
    2042: "Dave",
    2043: "Carl",
    2045: "Sam",
    2046: "Peter",
    2047: "Fred",
    2048: "Trevor"
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
    2048: "tester"
}

bot0_records = []
bot6_records = []

for filename in os.listdir(folder_path):
    if not filename.endswith('.csv') or 'Vs' not in filename:
        continue

    filepath = os.path.join(folder_path, filename)
    df = pd.read_csv(filepath)

    bot0_tag, bot6_tag = filename.replace('.csv', '').split('Vs')

    bot0_id = next((id for id, name in bot_name_map.items() if name.lower() == bot0_tag[:-1].lower()), None)
    bot6_id = next((id for id, name in bot_name_map.items() if name.lower() == bot6_tag[:-1].lower()), None)

    if bot0_id is None or bot6_id is None:
        continue

    bot0_strat = strat_name_map[bot0_id]
    bot6_strat = strat_name_map[bot6_id]
    is_self_play = bot0_tag[:-1] == bot6_tag[:-1]

    session_id = filename

    for _, row in df.iterrows():
        is_duplicate_buy = (
            row['user1_id'] == row['user2_id'] and
            row['choice_type'] == 'Buy' and
            row['choice_type2'] == 'Buy'
        )

        pid1, choice1, points1 = row['user1_id'], row['choice_type'], row['m_points']
        pid2, choice2, points2 = row['user2_id'], row['choice_type2'], row['m_points2']

        if pid1 == bot0_id:
            money = points1 - 500 if choice1 == 'Buy' else points1
            bot0_records.append({'Strategy': bot0_strat, 'Choice': choice1, 'Money': money, 'Session': session_id})
        elif pid1 == bot6_id or (is_self_play and pid1 == 2051):
            money = points1 - 500 if choice1 == 'Buy' else points1
            bot6_records.append({'Strategy': bot6_strat, 'Choice': choice1, 'Money': money, 'Session': session_id})

        if is_duplicate_buy:
            continue

        if pid2 == bot0_id:
            money = points2 - 500 if choice2 == 'Buy' else points2
            bot0_records.append({'Strategy': bot0_strat, 'Choice': choice2, 'Money': money, 'Session': session_id})
        elif pid2 == bot6_id or (is_self_play and pid2 == 2051):
            money = points2 - 500 if choice2 == 'Buy' else points2
            bot6_records.append({'Strategy': bot6_strat, 'Choice': choice2, 'Money': money, 'Session': session_id})

df_bot0 = pd.DataFrame(bot0_records)
df_bot6 = pd.DataFrame(bot6_records)

def compute_true_earnings(df):
    earnings = {}

    for strat, strat_group in df.groupby('Strategy'):
        total = 0

        for _, session_group in strat_group.groupby('Session'):
            session_group = session_group.reset_index(drop=True)
            prev = 100

            for _, row in session_group.iterrows():
                choice = row['Choice']
                money = row['Money']

                if choice == 'Buy':
                    prev = money
                    continue

                diff = money - prev
                total += diff
                prev = money

        earnings[strat] = total

    return pd.Series(earnings).sort_values(ascending=False)

earn0 = compute_true_earnings(df_bot0)
earn6 = compute_true_earnings(df_bot6)

choice0 = df_bot0.groupby(['Strategy', 'Choice']).size().unstack(fill_value=0)
choice6 = df_bot6.groupby(['Strategy', 'Choice']).size().unstack(fill_value=0)

choice0_norm = choice0.div(choice0.sum(axis=1), axis=0)
choice6_norm = choice6.div(choice6.sum(axis=1), axis=0)

sns.set(style="whitegrid")

plt.figure(figsize=(10, 5))
ax = sns.barplot(x=earn0.index, y=earn0.values)
plt.title("Total Earnings by Strategy (Bot0)")
plt.xlabel("Strategy")
plt.ylabel("Total Money")
plt.xticks(rotation=45)
for bar in ax.patches:
    height = bar.get_height()
    ax.text(
        bar.get_x() + bar.get_width() / 2,
        height + 5,
        f'{int(height)}',
        ha='center',
        va='bottom',
        fontsize=9
    )
plt.tight_layout()
plt.show()

plt.figure(figsize=(10, 5))
ax = sns.barplot(x=earn6.index, y=earn6.values)
plt.title("Total Earnings by Strategy (Bot6)")
plt.xlabel("Strategy")
plt.ylabel("Total Money")
plt.xticks(rotation=45)
for bar in ax.patches:
    height = bar.get_height()
    ax.text(
        bar.get_x() + bar.get_width() / 2,
        height + 5,
        f'{int(height)}',
        ha='center',
        va='bottom',
        fontsize=9
    )
plt.tight_layout()
plt.show()

fig, ax = plt.subplots(figsize=(10, 6))
ordered_cols_bot0 = ['Coop', 'Deflect']
choice0_norm = choice0_norm.reindex(columns=ordered_cols_bot0, fill_value=0)
bars = choice0_norm.plot(kind='bar', stacked=True, ax=ax)

# Add raw counts as annotations
for idx, strategy in enumerate(choice0_norm.index):
    y_offset = 0
    for col in ordered_cols_bot0:
        height = choice0_norm.loc[strategy, col]
        raw_value = choice0.loc[strategy, col] if col in choice0.columns else 0
        if height > 0:
            ax.text(idx, y_offset + height / 2, str(raw_value), ha='center', va='center', fontsize=9)
            y_offset += height

ax.set_title("Choice Distribution by Strategy (Bot0)")
ax.set_xlabel("Strategy")
ax.set_ylabel("Proportion")
plt.xticks(rotation=45)
plt.legend(title="Choice")
plt.tight_layout()
plt.show()

fig, ax = plt.subplots(figsize=(10, 6))
ordered_cols_bot6 = ['Coop', 'Deflect', 'Buy']
choice6_norm = choice6_norm.reindex(columns=ordered_cols_bot6, fill_value=0)
bars = choice6_norm.plot(kind='bar', stacked=True, ax=ax)

# Add raw counts as annotations
for idx, strategy in enumerate(choice6_norm.index):
    y_offset = 0
    for col in ordered_cols_bot6:
        height = choice6_norm.loc[strategy, col]
        raw_value = choice6.loc[strategy, col] if col in choice6.columns else 0
        if height > 0:
            ax.text(idx, y_offset + height / 2, str(raw_value), ha='center', va='center', fontsize=9)
            y_offset += height

ax.set_title("Choice Distribution by Strategy (Bot6)")
ax.set_xlabel("Strategy")
ax.set_ylabel("Proportion")
plt.xticks(rotation=45)
plt.legend(title="Choice")
plt.tight_layout()
plt.show()

print("=== Summary: Total Earnings ===")
print("Bot0:\n", earn0)
print("\nBot6:\n", earn6)

print("\n=== Summary: Choice Counts ===")
print("Bot0:\n", choice0)
print("\nBot6:\n", choice6)

def debug_strategy(df, strategy_name, role_label):
    group = df[df['Strategy'] == strategy_name].copy()
    if group.empty:
        print(f"\n No data found for {strategy_name} in {role_label}")
        return

    print(f"\n🔍 Step-by-step for '{strategy_name}' in {role_label}:")
    total = 0

    for session_name, session_group in group.groupby('Session'):
        session_group = session_group.reset_index(drop=True)
        prev = 100
        print(f"\n--- Session: {session_name} ---")

        for i, row in session_group.iterrows():
            choice = row['Choice']
            money = row['Money']

            if choice == 'Buy':
                print(f"Row {i}: BUY → Reset prev to {money} (Money after -500 already)")
                prev = money
                continue

            diff = money - prev
            total += diff
            print(f"Row {i}: Choice={choice}, Money={money}, Prev={prev}, Diff={diff}, Total={total}")
            prev = money

    print(f"\n✅ Final total earnings for {strategy_name} in {role_label}: {total}")

#debug_strategy(df_bot0, 'alwaysdefect', 'Bot0')
#debug_strategy(df_bot6, 'alwaysdefect', 'Bot6')

# Rename indices to indicate role
earn0_labeled = earn0.rename(lambda s: f"{s} (Bot0)")
earn6_labeled = earn6.rename(lambda s: f"{s} (Bot6)")

# Combine into a single Series
split_earnings = pd.concat([earn0_labeled, earn6_labeled])

# Filter out zero or negative earnings to avoid pie chart errors
split_earnings = split_earnings[split_earnings > 0]

# Plot the split pie chart
plt.figure(figsize=(10, 8))
plt.pie(
    split_earnings,
    labels=split_earnings.index,
    autopct='%1.1f%%',
    startangle=140
)
plt.title("Market Share by Strategy and Role (Bot0 vs Bot6)")
plt.axis('equal')
plt.tight_layout()
plt.show()

