using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins {
  [Info("Player Activity", "RayMods", "0.2.0")]
  [Description("Tracks player activity and AFK time.")]
  class PlayerActivity : RustPlugin {
    private DynamicConfigFile _playerData;
    private Dictionary<string, SessionData> _playerSessions = new Dictionary<string, SessionData>();
    private Dictionary<string, ActivityData> _activityDataCache = new Dictionary<string, ActivityData>();
    private Dictionary<string, Timer> _playerTimers = new Dictionary<string, Timer>();
    private Timer _saveTimer;
    private PluginConfig _config;


    #region Hooks

    private void Init() {
      _config = Config.ReadObject<PluginConfig>();
      _playerData = Interface.Oxide.DataFileSystem.GetFile("PlayerActivity");
    }

    private void OnServerInitialized() {
      _saveTimer = timer.Repeat(_config.SAVE_INTERVAL, 0, SaveActivityData);
    }
    
    private void OnUserConnected(BasePlayer player) {
      InitPlayer(player);
      _activityDataCache[player.UserIDString].LastConnection = DateTime.UtcNow;
    }

    private void OnUserDisconnected(BasePlayer player) {
      _playerSessions.Remove(player.UserIDString);
      _playerTimers[player.UserIDString].Destroy();
      _playerTimers.Remove(player.UserIDString);
    }

    private void Loaded() {
      BootstrapPlayerData();
      foreach (BasePlayer player in Player.Players) {
        InitPlayer(player);
      }
    }

    private void Unload() {
      _saveTimer.Destroy();
      foreach (Timer playerTimer in _playerTimers.Values) {
        playerTimer.Destroy();
      }
      foreach (BasePlayer player in Player.Players) {
        UpdatePlayerSession(player);
      }
      SaveActivityData();

      _playerTimers.Clear();
      _playerSessions.Clear();
      _activityDataCache.Clear();
    }

    protected override void LoadDefaultConfig() {
      Config.WriteObject(GetDefaultConfig(), true);
    }

    #endregion


    #region DataMgmt

    private void InitPlayer(BasePlayer player) {
      Puts($"init for player {player.displayName}");
      InitCache(player);
      InitSession(player);

      Timer playerTimer = timer.Repeat(_config.STATUS_CHECK_INTERVAL, 0, () => UpdatePlayerSession(player));
      _playerTimers.Add(player.UserIDString, playerTimer);
    }

    private void InitSession(BasePlayer player) {
       if (!_playerSessions.ContainsKey(player.UserIDString)) {
        _playerSessions.Add(player.UserIDString, new SessionData {
          PlayTime = 0,
          IdleTime = 0,
          ConnectionTime = DateTime.UtcNow,
          LastUpdateTime = DateTime.UtcNow
        });
      }
    }

    private void InitCache(BasePlayer player) {
      if (!_activityDataCache.ContainsKey(player.UserIDString)) {
        _activityDataCache.Add(player.UserIDString, new ActivityData {
          FirstConnection = DateTime.UtcNow,
          IdleTime = 0,
          LastConnection = DateTime.UtcNow,
          PlayTime = 0
        });
      }
    }

    private void BootstrapPlayerData() {
      RawActivityData rawData = _playerData.ReadObject<RawActivityData>();
      _activityDataCache = rawData.PlayerActivityData;
    }

    private void SaveActivityData() {
      RawActivityData rawDataForSave = new RawActivityData {
        PlayerActivityData = _activityDataCache,
      };
      _playerData.WriteObject(rawDataForSave);
    }

    private void UpdatePlayerSession(BasePlayer player) {
      bool isIdle = Player.IsConnected(player) && player.IdleTime > _config.AFK_TIMEOUT;
      DateTime lastUpdate = _playerSessions[player.UserIDString].LastUpdateTime;
      double secondsSinceLastUpdate = DateTime.UtcNow.Subtract(lastUpdate).TotalSeconds;

      if (isIdle) {
        _activityDataCache[player.UserIDString].IdleTime += secondsSinceLastUpdate;
        _playerSessions[player.UserIDString].IdleTime += secondsSinceLastUpdate;
      } else {
        _activityDataCache[player.UserIDString].PlayTime += secondsSinceLastUpdate;
        _playerSessions[player.UserIDString].PlayTime += secondsSinceLastUpdate;
      }
      _playerSessions[player.UserIDString].LastUpdateTime = DateTime.UtcNow;
    }

    #endregion


    #region API

    private Nullable<DateTime> GetFirstConnectionDate(string playerId) {
      if (_activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].FirstConnection;
      }
      return null;
    }

    private Nullable<DateTime> GetLastConnectionDate(string playerId) {
      if (_activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].LastConnection;
      };
      return null;
    }

    private Nullable<double> GetTotalPlayTime(string playerId) {
      if (_activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].PlayTime;
      }
      return null;
    }

    private Nullable<double> GetTotalIdleTime(string playerId) {
      if (_activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].IdleTime;
      }
      return null;
    }

    private Nullable<double> GetSessionPlayTime(string playerId) {
      if (_playerSessions.ContainsKey(playerId)) {
        return _playerSessions[playerId].PlayTime;
      }
      return null;
    }

    private Nullable<double> GetSessionIdleTime(string playerId) {
      if (_playerSessions.ContainsKey(playerId)) {
        return _playerSessions[playerId].IdleTime;
      }
      return null;
    }

    private Nullable<DateTime> GetSessionStartTime(string playerId) {
      if (_playerSessions.ContainsKey(playerId)) {
        return _playerSessions[playerId].ConnectionTime;
      }
      return null;
    }

    #endregion


    #region Utilities

    private PluginConfig GetDefaultConfig() {
      return new PluginConfig {
        AFK_TIMEOUT = 300,
        SAVE_INTERVAL = 900,
        STATUS_CHECK_INTERVAL = 60
      };
    }

    #endregion


    private class PluginConfig {
      public int AFK_TIMEOUT;
      public int SAVE_INTERVAL;
      public int STATUS_CHECK_INTERVAL;
    }

    private class RawActivityData {
      public Dictionary<string, ActivityData> PlayerActivityData = new Dictionary<string, ActivityData>();
    }

    private class ActivityData {
      public double PlayTime;
      public double IdleTime;
      public DateTime FirstConnection;
      public DateTime LastConnection;
    }

    private class SessionData {
      public double PlayTime;
      public double IdleTime;
      public DateTime ConnectionTime;
      public DateTime LastUpdateTime;
    }
  }
}
