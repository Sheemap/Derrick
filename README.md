# Derrick

Derrick is a fun Discord bot who aggregates match data for various games, and sends out periodic awards for users who played the best.

## Setup

Derrick is integrated with Discord slash commands, so you can use Discord to help you configure him. Just type `/setup` in your server, and Discord will help guide you along the options. There are only 3, so it shouldn't be too hard.

Heres a quick overview of the options:

* **channel** - The channel to send the awards in. Discord will open a channel select menu for you
* **game** - The game to grab stats for. Discord will show you the options available
* **schedule** - A cron string, this specifies when Derrick should send the awards. If not specified, he will send them at midnight UTC on Tuesday. Schedules will be processed in UTC time

Once Derrick is setup on a channel, your work as an admin is over! You can optionally add him to more if you'd like, but Derrick will function now.

For user's to be included in awards, they need to first join the channel, then link any game accounts they want.

To join the channel, users just use `/join`. Derrick will do his best to auto join them, but will pop up easy buttons if more info is needed.

To link accounts, users just use `/link`. They must provide their game account ID, and the game they want to link. There's no verification yet, but this is a planned feature.

The game account ID is different based on the game. For Dota it is their Steam32 ID, for league it is their Summoner name

Once a user has joined the channel and linked accounts, they are good to go! They will be included in the awards.
