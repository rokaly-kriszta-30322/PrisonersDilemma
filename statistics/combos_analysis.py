import pandas as pd
import glob
import os
import matplotlib.pyplot as plt
import seaborn as sns

folder_path = './data/combos'
csv_files = glob.glob(os.path.join(folder_path, '*.csv'))

bot_names = {
    2039: "titfortat",
    2040: "sneaky",
    2041: "random",
    2042: "alwaysdefect",
    2043: "alwayscoop",
    2045: "twotitfortat",
    2046: "probe",
    2047: "grudge",
    2048: "tester"
}

all_sessions = []
for file in csv_files:
    df = pd.read_csv(file)

    df = df.head(1000)

    bot1 = df['user1_id'].iloc[0]
    bot2 = df['user2_id'].iloc[0]

    df['Bot1'] = bot1
    df['Bot2'] = bot2

    all_sessions.append(df)

df_all = pd.concat(all_sessions, ignore_index=True)

df_user1 = df_all[['user1_id', 'choice_type', 'm_points', 'Bot1', 'Bot2']].rename(columns={
'user1_id': 'BotID', 'choice_type': 'Choice', 'm_points': 'MoneyPoints'
})
df_user2 = df_all[['user2_id', 'choice_type2', 'm_points2', 'Bot2', 'Bot1']].rename(columns={
    'User2': 'BotID', 'Choice2': 'Choice', 'MoneyPoints2': 'MoneyPoints'
})

df_combined = pd.concat([df_user1, df_user2], ignore_index=True)

df_combined['BotID'] = df_combined['BotID'].map(bot_names).fillna(df_combined['BotID'])

bot_summary = df_combined.groupby('BotID').agg(
    TotalMoney=('MoneyPoints', 'sum'),
    AvgMoney=('MoneyPoints', 'mean'),
    CoopCount=('Choice', lambda x: (x == 'Coop').sum()),
    DeflectCount=('Choice', lambda x: (x == 'Deflect').sum()),
    BuyCount=('Choice', lambda x: (x == 'Buy').sum()),
    TotalRounds=('Choice', 'count')
)

bot_summary['CoopRate'] = bot_summary['CoopCount'] / bot_summary['TotalRounds']
bot_summary['DeflectRate'] = bot_summary['DeflectCount'] / bot_summary['TotalRounds']
bot_summary['BuyRate'] = bot_summary['BuyCount'] / bot_summary['TotalRounds']

sns.set(style="whitegrid")

bot_summary_sorted = bot_summary.sort_values(by="TotalMoney", ascending=False)

plt.figure(figsize=(12, 6))
sns.barplot(x=bot_summary_sorted.index.astype(str), y="TotalMoney", data=bot_summary_sorted)
plt.title("Total Money Earned by Bot")
plt.xlabel("Bot ID")
plt.ylabel("Total Money")
plt.xticks(rotation=45)
plt.tight_layout()
plt.show()

plt.figure(figsize=(12, 6))
bot_summary_sorted[['CoopRate', 'DeflectRate', 'BuyRate']].plot(kind='bar', stacked=True)
plt.title("Choice Distribution by Bot")
plt.xlabel("Bot ID")
plt.ylabel("Rate")
plt.xticks(rotation=45)
plt.legend(title="Choice Type")
plt.tight_layout()
plt.show()

df_combined['Round'] = df_combined.groupby('BotID').cumcount() + 1
df_combined['CumulativeMoney'] = df_combined.groupby('BotID')['MoneyPoints'].cumsum()

plt.figure(figsize=(14, 7))
sns.lineplot(data=df_combined, x='Round', y='CumulativeMoney', hue='BotID', palette='tab10')
plt.title("Cumulative Money Over Time per Bot")
plt.xlabel("Number of Interactions")
plt.ylabel("Cumulative Money")
plt.legend(title="Bot ID", bbox_to_anchor=(1.05, 1), loc='upper left')
plt.tight_layout()
plt.show()