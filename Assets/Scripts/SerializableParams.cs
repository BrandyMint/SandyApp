using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public abstract class SerializableParams {
    public event Action OnChanged;
    
    [JsonIgnore]
    public bool HasChanges => _params.Values.Any(p => p.IsChanged);
    [JsonIgnore]
    public bool HasFile { get; private set; }

    protected class ParamCache {
        public object val;
        public object oldVal;
        public bool IsChanged => !Equals(val, oldVal);

        public ParamCache(object val) {
            this.val = this.oldVal = val;
        }
    }
    
    protected virtual string GetFileName() {
        return GetType().Name + ".json";
    }

    protected string _path;
    protected bool _invokeChangedOnSetting = true;
    protected bool _isLoaded;
    protected bool _forceResetOnGet;
    protected readonly Dictionary<string, ParamCache> _params = new Dictionary<string, ParamCache>();

    protected virtual string GetFullPath() {
        if (_path == null)
            _path = Application.persistentDataPath;
        return Path.Combine(_path, GetFileName());
    }

    public bool Load(bool invokeChanged = true) {
        _isLoaded = true;
        try {
            _invokeChangedOnSetting = false;
            var json = File.ReadAllText(GetFullPath());
            JsonConvert.PopulateObject(json, this);
            HasFile = true;
        } catch (Exception e) {
            //Debug.LogWarning(e);
            return false;
        } finally {
            _invokeChangedOnSetting = true;
        }
        
        if (invokeChanged)
            InvokeChanged();
        return true;
    }

    public bool Save() {
        try {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetFullPath(), json);
            foreach (var cache in _params.Values) {
                cache.oldVal = cache.val;
            }
            HasFile = true;
            return true;
        } catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }

    public void Reset() {
        _forceResetOnGet = true;
        _invokeChangedOnSetting = false;
        var json = JsonConvert.SerializeObject(this, Formatting.None);
        JsonConvert.PopulateObject(json, this);
        _invokeChangedOnSetting = true;
        _forceResetOnGet = false;
        InvokeChanged();
    }

    protected virtual void Set(string paramName, object val) {
        var valIsLoaded = _params.TryGetValue(paramName, out var cache);
        if (valIsLoaded) {
            cache.val = val;
        } else {
            _params[paramName] = new ParamCache(val);
        }
        if (_invokeChangedOnSetting)
            InvokeChanged();
    }

    protected virtual T Get<T>(string paramName, T defVal = default(T)) {
        if (!_forceResetOnGet) {
            if (!_isLoaded)
                Load(false);
            if (_params.TryGetValue(paramName, out var val)) {
                return (T) val.val;
            }
        }
        return defVal;
    }

    protected virtual void InvokeChanged() {
        OnChanged?.Invoke();
    }
}