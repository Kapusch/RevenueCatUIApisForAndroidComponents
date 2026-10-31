plugins { id("com.android.library"); kotlin("android") }

android {
    namespace = "com.kapusch.revenuecatui.androidinterop"
    compileSdk = 35
    // purchases-ui declares minSdk 24; the wrapper cannot be lower than its dependency.
    defaultConfig { minSdk = 24; consumerProguardFiles("consumer-rules.pro") }
    compileOptions { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
}

kotlin { compilerOptions { jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17) } }

dependencies {
    // The BOM aligns every Compose artifact with the Compose bindings the package references
    // (Xamarin.AndroidX.Compose.* 1.10.4.x, Material3 1.4.0.x).
    implementation(platform("androidx.compose:compose-bom:2026.02.01"))
    implementation("com.revenuecat.purchases:purchases-ui:10.16.1")
    // PaywallView extends AbstractComposeView, but purchases-ui only declares Compose at runtime.
    compileOnly("androidx.compose.ui:ui")
}

// Only modules without a managed .NET binding on the consumer's AndroidX wave are shipped.
// Everything else (RevenueCat core, Compose, AndroidX, Kotlin, OkHttp) comes from NuGet packages,
// to avoid duplicate Java classes. Files are renamed to <artifact>-<version> to stay unique.
val nativeModules = setOf(
    "com.revenuecat.purchases:purchases-ui",
    "androidx.compose.ui:ui-text-google-fonts",
    "androidx.compose.material3.adaptive:adaptive-android",
    "androidx.constraintlayout:constraintlayout-compose-android",
    "io.coil-kt:coil",
    "io.coil-kt:coil-base",
    "io.coil-kt:coil-compose",
    "io.coil-kt:coil-compose-base",
    "io.coil-kt:coil-svg",
    "com.google.accompanist:accompanist-drawablepainter",
    "com.caverock:androidsvg-aar",
    "org.commonmark:commonmark",
    "org.commonmark:commonmark-ext-gfm-strikethrough",
)

fun runtimeArtifacts() =
    configurations.getByName("releaseRuntimeClasspath").resolvedConfiguration.resolvedArtifacts
        .distinctBy { it.file }
        .sortedBy { it.moduleVersion.id.toString() }

tasks.register("listRuntimeArtifacts") {
    doLast {
        val lines = runtimeArtifacts().map { "${it.moduleVersion.id} ${it.file.name}" }
        lines.forEach { println(it) }
        val out = rootProject.layout.projectDirectory.dir("build").asFile
        out.mkdirs()
        out.resolve("runtime-artifacts.txt").writeText(lines.joinToString("\n", postfix = "\n"))
        out.resolve("native-modules.txt").writeText(nativeModules.sorted().joinToString("\n", postfix = "\n"))
    }
}

tasks.register("copyNativeDependencies") {
    doLast {
        val out = rootProject.layout.projectDirectory.dir("build/deps").asFile
        out.mkdirs()
        val shipped = runtimeArtifacts().filter { "${it.moduleVersion.id.group}:${it.moduleVersion.id.name}" in nativeModules }
        val missing = nativeModules - shipped.map { "${it.moduleVersion.id.group}:${it.moduleVersion.id.name}" }.toSet()
        check(missing.isEmpty()) { "Native modules not resolved: $missing" }
        shipped.forEach {
            it.file.copyTo(out.resolve("${it.moduleVersion.id.name}-${it.moduleVersion.id.version}.${it.extension}"), overwrite = true)
        }
    }
}
