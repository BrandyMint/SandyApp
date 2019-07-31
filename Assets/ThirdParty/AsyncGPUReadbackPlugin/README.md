# AsyncGPUReadbackPlugin
On Unity 2018.2 was introduced a really neat feature: being able get a frame from the gpu to the cpu without blocking the rendering. This feature is really useful for screenshot or network stream of a game camera because we need the frame on the cpu, but we don't care if there is a little delay.

However this feature is only available on platform supporting DirectX (Windows) and Metal (Apple), but not OpenGL and it's not planned. (source: https://forum.unity.com/threads/graphics-asynchronous-gpu-readback-api.529901/#post-3487735).

This plugin aim to provide this feature for OpenGL platform. It tries to match the official AsyncGPUReadback as closes as possible to let you easily switch between the plugin or the official API. Under the hood, it use the official API if available on the current platform.

## Use it
### Install
Copy `UnityExampleProject/Assets/AsyncGPUReadbackPlugin` to anywhere under your `/Assets` folder.

### The API
Once you copied the plugin, add `using AsyncGPUReadbackPluginNs` at the beginning of the script where you want to use it.

#### `static AsyncGPUReadbackPluginRequest AsyncGPUReadback.Request(Texture src)`
Same as the official API except that it doesn't implement all the other form. It request the texture from the gpu and return a `AsyncGPUReadbackPluginRequest` object to let you watch the state of the operation and get data back.

#### `AsyncGPUReadbackPluginRequest`
This object let you see if the request is done and get the data you asked for.

##### Attributes

* `hasError`: True if the request failed
* `done`: True if the request is done

##### Methods

* `NativeArray<T> GetData<T>()`: This let you get the data you asked for in the format you want once it is available.
* `void Update(bool force = false)`: This method has to be called regularly to refresh request state. It called automatically by `AsyncGPURequestLifeTimeManager`. It will do nothing if the official API is used and if `force == false`.
* `void Dispose()`: Free plugins buffer. It called automatically by `AsyncGPURequestLifeTimeManager` on next frame after request is done.

### Example
To see a working example you can open `UnityExampleProject` with the Unity editor. It saves screenshot of the camera every 60 frames. The script taking screenshot is in `UnityExampleProject/Scripts/UsePlugin.cs`

### Differences with the official API
Not implemented:

* ComputeBuffer source 
* size and offset of source
* specifying destination format

## Build it!

### Native plugin
```
cd NativePlugin
make # The makefile only work for linux, but you could add other target inside if you want
```
You can find the built file under
```
NativePlugin/build/libAsyncGPUReadbackPlugin.so
```

## Thanks

* https://github.com/Alabate/AsyncGPUReadbackPlugin
