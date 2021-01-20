# PlayerActivity

**PlayerActivity** is a no-frills activity data provider which tracks play and idle time for players on a rust server.

## Configuration

```
{
  "AFK_TIMEOUT": 300,
  "SAVE_INTERVAL: 300,
  "STATUS_CHECK_INTERVAL": 60
}
```

## API

### GetFirstConnectionDate
```
PlayerActivity.Call("GetFirstConnectionDate", playerId);
```
Returns the UTC DateTime of the player's first connection to the server.

### GetLastConnectionDate
```
PlayerActivity.Call("GetLastConnectionDate", playerId);
```
Returns the UTC DateTime of the player's most recent connection to the server.

### GetTotalPlayTime
```
PlayerActivity.Call("GetTotalPlayTime", playerId);
```
Returns the player's total play time on the server in seconds.

### GetTotalIdleTime
```
PlayerActivity.Call("GetTotalIdleTime", playerId);
```
Returns the player's total idle time on the server in seconds.

### GetSessionPlayTime
```
PlayerActivity.Call("GetSessionPlayTime", playerId);
```
Returns the player's play time during the current session in seconds.

### GetSessionIdleTime
```
PlayerActivity.Call("GetSessionIdleTime", playerId);
```
Returns the player's idle time during the current session in seconds.

### GetSessionStartTime
```
PlayerActivity.Call("GetSessionStartTime", playerId);
```
Returns the UTC DateTime of beginning of the player's current session.

### GetIsAfk
```
PlayerActivity.Call("GetIsAfk", playerId);
```
Returns a boolean value representing whether the player is currently online and AFK.
