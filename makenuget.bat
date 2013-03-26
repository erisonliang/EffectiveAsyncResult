copy EffectiveAsyncResult\bin\Release\EffectiveAsyncResult.dll nuget\lib\net40
copy EffectiveAsyncResult.net45\bin\Release\EffectiveAsyncResult.net45.dll nuget\lib\net45
pushd nuget
..\util\nuget.exe pack EffectiveAsyncResult.nuspec 
popd
