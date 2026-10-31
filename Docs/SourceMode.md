# Source builds

Run `bash src/Kapusch.RevenueCatUIApisForAndroidComponents/Native/Android/build.sh`, then pack. The
script bootstraps Gradle 8.11.1 (SHA-256 checked), builds the `revenuecatuiinterop` facade AAR,
copies the purchases-ui dependencies that have no managed binding into `build/deps` and checks them
against `DependencyLocks/native-artifacts.json`. All downloads happen here, never in the consumer
target.

Source mode uses `UseKapuschRevenueCatUIAndroidInteropFromSource=true` and imports the matching
buildTransitive target explicitly when using a ProjectReference.

Qualification toolchain: .NET SDK and workload set **10.0.203**
(`dotnet workload install android --version 10.0.203`), JDK 17 and Android SDK platform 35.

## Updating purchases-ui

purchases-ui must match the core binding's purchases AAR exactly.

1. Wait for (or contribute) a `Kebechet.Maui.RevenueCat.Android` release of the new version.
2. Update `purchases-ui` in `revenuecatuiinterop/build.gradle.kts`, the
   `Kebechet.Maui.RevenueCat.Android` range and file checks in the csproj and targets,
   `THIRD_PARTY_NOTICES.md`, `nuget-readme.md`, `README.md` and `CHANGELOG.md`.
3. Run `./gradlew :revenuecatuiinterop:listRuntimeArtifacts` (through `build.sh`'s Gradle) and check
   that every module without a managed binding is in `nativeModules`.
4. If the consumer's AndroidX wave changes, take every `Xamarin.AndroidX.*` version from the
   dotnet/android-libraries release of that wave and set the Compose BOM with the same Compose UI and
   Material3 versions.
5. Delete `DependencyLocks/native-artifacts.json`, rebuild to regenerate it, review the file list and
   commit it. Then run `scripts/verify-dependency-lock.py` and `scripts/verify-package.py`.
