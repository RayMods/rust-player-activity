using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins {
  [Info("Player Activity", "RayMods", "0.1.0")]
  [Description("Tracks player connection activity details")]
  class PlayerActivity : RustPlugin {
    private const int AFK_TIMER = 60;
    private const int SAVE_INTERVAL = 10; // seconds
    private const int STATUS_CHECK_INTERVAL = 10;

    private DynamicConfigFile _playerData;

    private Dictionary<string, SessionData> _newSessionData = new Dictionary<string, SessionData>();
    private Dictionary<string, Timer> _sessionGenLoops = new Dictionary<string, Timer>();

    private Dictionary<string, SessionData> _playerSessions = new Dictionary<string, SessionData>();
    private Dictionary<string, ActivityData> _activityDataCache = new Dictionary<string, ActivityData>();
    private Dictionary<string, Timer> _playerTimers = new Dictionary<string, Timer>();
    private Timer _saveTimer;


    #region Hooks

    private void Init() {
      _playerData = Interface.Oxide.DataFileSystem.GetFile("PlayerActivity");
    }

    private void OnServerInitialized() {
      _saveTimer = timer.Repeat(SAVE_INTERVAL, 0, SaveActivityData);
    }
    
    private void OnUserConnected(BasePlayer player) {
      InitPlayer(player);
      _activityDataCache[player.UserIDString].LastConnection = DateTime.UtcNow;
    }

    private void OnUserDisconnected(BasePlayer player) {
      _newSessionData.Remove(player.UserIDString);
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
      _newSessionData.Clear();
      _activityDataCache.Clear();
    }

    #endregion


    #region DataMgmt

    private void InitPlayer(BasePlayer player) {
      InitCache(player);
      InitSession(player);

      Timer playerTimer = timer.Repeat(STATUS_CHECK_INTERVAL, 0, () => UpdatePlayerSession(player));
      _playerTimers.Add(player.UserIDString, playerTimer);
    }

    private void InitSession(BasePlayer player) {
       if (!_newSessionData.ContainsKey(player.UserIDString)) {
        _newSessionData.Add(player.UserIDString, new SessionData {
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
      bool isIdle = player.IdleTime > AFK_TIMER;
      DateTime lastUpdate = _newSessionData[player.UserIDString].LastUpdateTime;
      double secondsSinceLastUpdate = DateTime.UtcNow.Subtract(lastUpdate).TotalSeconds;

      if (isIdle) {
        _activityDataCache[player.UserIDString].IdleTime += secondsSinceLastUpdate;
        _newSessionData[player.UserIDString].IdleTime += secondsSinceLastUpdate;
      } else {
        _activityDataCache[player.UserIDString].PlayTime += secondsSinceLastUpdate;
        _newSessionData[player.UserIDString].PlayTime += secondsSinceLastUpdate;
      }
      _newSessionData[player.UserIDString].LastUpdateTime = DateTime.UtcNow;
    }

    #endregion


    #region API

    public Nullable<DateTime> GetFirstConnectionDate(string playerId) {
      if (playerId != null && _activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].FirstConnection;
      }
      return null;
    }

    public Nullable<DateTime> GetLastConnectionDate(string playerId) {
      if (_activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].LastConnection;
      };
      return null;
    }

    public Nullable<double> GetTotalPlayTime(string playerId) {
      if (player != null && _activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].PlayTime;
      }
      return null;
    }

    public Nullable<double> GetTotalIdleTime(string playerId) {
      if (_activityDataCache.ContainsKey(playerId)) {
        return _activityDataCache[playerId].IdleTime;
      }
      return null;
    }

    public Nullable<double> GetSessionPlayTime(string playerId) {
      if (_newSessionData.ContainsKey(playerId)) {
        return _newSessionData[playerId].PlayTime;
      }
      return null;
    }

    public Nullable<double> GetSessionIdleTime(string playerId) {
      if (_newSessionData.ContainsKey(playerId)) {
        return _newSessionData[playerId].IdleTime;
      }
      return null;
    }

    public Nullable<DateTime> GetSessionStartTime(string playerId) {
      if (_newSessionData.ContainsKey(playerId)) {
        return _newSessionData[playerId].ConnectionTime;
      }
      return null;
    }

    #endregion

    


    private class RawActivityData {
      public Dictionary<string, ActivityData> PlayerActivityData;
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


    #region Utilities
    #endregion
  }
}