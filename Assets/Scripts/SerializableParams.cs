using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public abstract class SerializableParams {
    public event Action OnChanged;
    
    protected virtual string GetFileName() {
        return GetType().Name + ".json";
    }

    protected string _path;
    protected bool _invokeChangedOnSetting = true;
    protected bool _isLoaded = false;
    protected readonly Dictionary<string, object> _params = new Dictionary<string, object>();

    protected virtual string GetFullPath() {
        if (_path == null)
            _path = Application.persistentDataPath;
        return Path.Combine(_path, GetFileName());
    }

    public void Load(bool invokeChanged = true) {
        _isLoaded = true;
        try {
            _invokeChangedOnSetting = false;
            var json = File.ReadAllText(GetFullPath());
            JsonConvert.PopulateObject(json, this);
        } catch (Exception e) {
            Debug.LogException(e);
            return;
        } finally {
            _invokeChangedOnSetting = true;
        }
        
        if (invokeChanged)
            InvokeChanged();
    }

    public bool Save() {
        try {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetFullPath(), json);
            return true;
        } catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }

    public void Reset() {
        _params.Clear();
        InvokeChanged();
    }

    protected virtual void Set(string paramName, object val) {
        _params[paramName] = val;
        if (_invokeChangedOnSetting)
            InvokeChanged();
    }

    protected virtual T Get<T>(string paramName, T defVal = default(T)) {
        if (!_isLoaded)
            Load(false);
        if (_params.TryGetValue(paramName, out var val)) {
            return (T) val;
        }
        return defVal;
    }

    protected virtual void InvokeChanged() {
        OnChanged?.Invoke();
    }
}