plugins {
    id("com.android.library") version "8.7.3" apply false
    // Compose 1.10 and Kotlin stdlib 2.3 metadata need a Kotlin 2.3 compiler to read.
    kotlin("android") version "2.3.10" apply false
}
