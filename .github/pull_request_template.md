## Summary

- What does this change do?

## Checklist

- [ ] No secrets committed
- [ ] Built native artifacts: `bash src/Kapusch.RevenueCatUIApisForAndroidComponents/Native/Android/build.sh`
- [ ] Packed NuGet: `dotnet pack src/Kapusch.RevenueCatUIApisForAndroidComponents/Kapusch.RevenueCatUIApisForAndroidComponents.csproj -c Release -o artifacts/nuget`
- [ ] Validated package and JNI contract: `python3 scripts/verify-package.py`
- [ ] Dependency lock check: `python3 scripts/verify-dependency-lock.py`
- [ ] Public tree scan: `python3 scripts/verify-public.py`
- [ ] Sample built: `dotnet build samples/Smoke/Smoke.csproj -c Release`
- [ ] Runtime coverage: `python3 scripts/verify-runtime-coverage.py`
- [ ] Updated docs if behavior/integration changed
