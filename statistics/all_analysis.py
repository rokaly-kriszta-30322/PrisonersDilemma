import pandas as pd

df_all_running = pd.read_csv('./data/all_in_one/AllRunning.csv')

bot_names = {
    2039: 'titfortat',
    2040: 'sneaky',
    2041: 'random',
    2042: 'alwaysdefect',
    2043: 'alwayscoop',
    2045: 'twotitfortat',
    2046: 'probe',
    2047: 'grudge',
    2048: 'tester'
}

df_user1 = df_all_running[['user1_id', 'choice_type', 'm_points']].rename(columns={
    'user1_id': 'BotID', 'choice_type': 'Choice', 'm_points': 'MoneyPoints'
})
df_user2 = df_all_running[['user2_id', 'choice_type2', 'm_points2']].rename(columns={
    'user2_id': 'BotID', 'choice_type2': 'Choice', 'm_points2': 'MoneyPoints'
})

df_combined = pd.concat([df_user1, df_user2], ignore_index=True)
df_combined['BotName'] = df_combined['BotID'].map(bot_names)
df_combined = df_combined[df_combined['BotName'].notna()]

df_combined['Round'] = df_combined.groupby('BotName').cumcount() + 1
df_combined['CumulativeMoney'] = df_combined.groupby('BotName')['MoneyPoints'].cumsum()

import seaborn as sns
import matplotlib.pyplot as plt

plt.figure(figsize=(14, 7))
sns.lineplot(data=df_combined, x='Round', y='CumulativeMoney', hue='BotName')
plt.title("Cumulative Money Over Time (All Bots Active)")
plt.xlabel("Round")
plt.ylabel("Cumulative Money")
plt.legend(title="Bot")
plt.tight_layout()
plt.show()

summary = df_combined.groupby('BotName').agg(
    TotalMoney=('MoneyPoints', 'sum'),
    AvgMoney=('MoneyPoints', 'mean'),
    TotalRounds=('MoneyPoints', 'count'),
    CoopCount=('Choice', lambda x: (x == 'Coop').sum()),
    DeflectCount=('Choice', lambda x: (x == 'Deflect').sum()),
    BuyCount=('Choice', lambda x: (x == 'Buy').sum()),
)

summary['CoopRate'] = summary['CoopCount'] / summary['TotalRounds']
summary['DeflectRate'] = summary['DeflectCount'] / summary['TotalRounds']
summary['BuyRate'] = summary['BuyCount'] / summary['TotalRounds']
print(summary.sort_values(by='TotalMoney', ascending=False))