# D3D12SDKVersionPatcher
A tool that can patch an .exe file exports (actually it will create exports as an exe usually doesn't have any) to enabled DirectX 12 Agility SDK versioning

To use it just run it in command line for example like this:

​    `D3D12SDKVersionPatcher "E:\MyPath\bin\Debug\net8.0-windows\MyExe.exe" 611 ".\D3D12"`

And it will patch the MyExe.exe with `D3D12SDKVersion` set to `611` and `D3D12SDKPath` set to `.\D3D12`
