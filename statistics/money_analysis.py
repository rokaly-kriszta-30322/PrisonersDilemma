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
    2045: "twotitsfortat",
    2046: "probe",
    2047: "grudge",
    2048: "tester",
    2049: "t4tminmoney",
    2050: "t4tmaxmoney"
}

all_sessions = []
for file in csv_files:
    df = pd.read_csv(file).head(1000)
    df['Bot1'] = df['user1_id'].iloc[0]
    df['Bot2'] = df['user2_id'].iloc[0]
    all_sessions.append(df)
df_all = pd.concat(all_sessions, ignore_index=True)

money_limit_files = glob.glob('./data/money_limit/*.csv')
money_sessions = []
for file in money_limit_files:
    df = pd.read_csv(file).head(1000)
    df['Bot1'] = df['user1_id'].iloc[0]
    df['Bot2'] = df['user2_id'].iloc[0]
    money_sessions.append(df)
df_money = pd.concat(money_sessions, ignore_index=True)

df_total = pd.concat([df_all, df_money], ignore_index=True)

def format_df(df, id_col, choice_col, money_col, bot_col1, bot_col2):
    return df[[id_col, choice_col, money_col, bot_col1, bot_col2]].rename(columns={
        id_col: 'BotID', choice_col: 'Choice', money_col: 'MoneyPoints',
        bot_col1: 'Bot1', bot_col2: 'Bot2'
    })

df_user1 = format_df(df_total, 'user1_id', 'choice_type', 'm_points', 'Bot1', 'Bot2')
df_user2 = format_df(df_total, 'user2_id', 'choice_type2', 'm_points2', 'Bot2', 'Bot1')
df_combined_all = pd.concat([df_user1, df_user2], ignore_index=True)

df_combined_all['BotID'] = df_combined_all['BotID'].map(bot_names).fillna(df_combined_all['BotID'])

df_combined_all['Round'] = df_combined_all.groupby('BotID').cumcount() + 1
df_combined_all['CumulativeMoney'] = df_combined_all.groupby('BotID')['MoneyPoints'].cumsum()

df_combined_all['BuyCost'] = df_combined_all['Choice'].apply(lambda x: 500 if x == 'Buy' else 0)
df_combined_all['CumulativeBuyCost'] = df_combined_all.groupby('BotID')['BuyCost'].cumsum()
df_combined_all['CumulativeNetGain'] = df_combined_all['CumulativeMoney'] - df_combined_all['CumulativeBuyCost']

bot_summary = df_combined_all.groupby('BotID').agg(
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
bot_summary['BuyCost'] = bot_summary['BuyCount'] * 500
bot_summary['NetGain'] = bot_summary['TotalMoney'] - bot_summary['BuyCost']

plt.figure(figsize=(12, 6))
bot_summary_sorted = bot_summary.sort_values(by="NetGain", ascending=False)
sns.barplot(x=bot_summary_sorted.index.astype(str), y="NetGain", data=bot_summary_sorted)
plt.title("Net Gain (Total Money - Buy Cost) by Bot")
plt.xlabel("Strategies")
plt.ylabel("Net Gain")
plt.xticks(rotation=45)
plt.tight_layout()
plt.show()

plt.figure(figsize=(12, 6))
bot_summary_sorted[['CoopRate', 'DeflectRate', 'BuyRate']].plot(kind='bar', stacked=True)
plt.title("Choice Distribution by Bot")
plt.xlabel("Strategies")
plt.ylabel("Rate")
plt.xticks(rotation=45)
plt.legend(title="Choice Type")
plt.tight_layout()
plt.show()

bots_to_plot = ['titfortat', 't4tminmoney', 't4tmaxmoney']
df_t4t_comparison = df_combined_all[df_combined_all['BotID'].isin(bots_to_plot)]

plt.figure(figsize=(14, 7))
sns.lineplot(data=df_t4t_comparison, x='Round', y='CumulativeMoney', hue='BotID', palette='Set1')
plt.title("Cumulative Money Over Time: Tit for Tat Variants")
plt.xlabel("Round")
plt.ylabel("Cumulative Money")
plt.legend(title="Bot Strategy")
plt.tight_layout()
plt.show()

plt.figure(figsize=(14, 7))
sns.lineplot(data=df_combined_all[df_combined_all['BotID'].isin(bots_to_plot)],
             x='Round', y='CumulativeNetGain', hue='BotID', palette='Set1')
plt.title("Cumulative Net Gain Over Time: Tit for Tat Variants")
plt.xlabel("Round")
plt.ylabel("Net Gain (Money - Buy Cost)")
plt.legend(title="Bot Strategy")
plt.tight_layout()
plt.show()

summary_t4t = bot_summary.loc[bots_to_plot][[
    'TotalMoney', 'BuyCount', 'BuyCost', 'NetGain', 'CoopRate', 'DeflectRate', 'BuyRate'
]].round(2)

print("\n=== Tit for Tat Variants Summary ===\n")
print(summary_t4t)