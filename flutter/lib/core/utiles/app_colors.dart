import 'package:flutter/material.dart';

class AppColors {
  AppColors._();

  // ---------------------------
  // Base palettes (your values)
  // ---------------------------

  // -- Dark Mode Palette --
  static const Color darkText = Color(0xFFFFFFFF);
  static const Color darkBackground = Color(0xFF1B1C30);
  static const Color darkSurface = Color(0xFF232337);

  // -- Light Mode Palette --
  static const Color lightText = Color(0xFF2D2C2C);
  static const Color lightBackground = Color(0xFFEFEFEF);
  static const Color lightSurface = Color(0xFFD6D5D9);

  // -- Brand/Status Colors --
  static const Color primary = Color(0xFF7857C7);
  static const Color success = Color(0xFF4CAF50);
  static const Color error = Color(0xFFF44336);
  static const Color warning = Color(0xFFFF9800);

  // -- Dark Navbar Colors --
  static const Color darkNavbar = Color(0xFF26273B);
  static const Color darkNavActive = primary;
  static const Color darkNavInactive = Color(0xFFB5B3B3);

  // -- Light Navbar Colors --
  static const Color lightNavbar = Color(0xFFD6CDEE);
  static const Color lightNavActive = primary;
  static const Color lightNavInactive = Color(0xFFAE9ADD);

  // ---------------------------
  // Theme-mode aware layer
  // ---------------------------

  static ThemeMode _mode = ThemeMode.system;

  /// Call this from ThemeCubit whenever the mode changes.
  static void setThemeMode(ThemeMode mode) {
    _mode = mode;
  }

  static bool get _isDark {
    if (_mode == ThemeMode.dark) return true;
    if (_mode == ThemeMode.light) return false;

    // ThemeMode.system
    final platform = WidgetsBinding.instance.platformDispatcher.platformBrightness;
    return platform == Brightness.dark;
  }

  // App-level semantic colors
  static Color get textColor => _isDark ? darkText : lightText;
  static Color get backgroundColor => _isDark ? darkBackground : lightBackground;
  static Color get surfaceColor => _isDark ? darkSurface : lightSurface;

  // ---------------------------
  // Legacy aliases used by your screens (no structure change needed)
  // ---------------------------

  static Color get mainColor => surfaceColor;
  static Color get whiteColor => textColor;
  static Color get primaryColor => primary;
  static Color get unSelectedColor => _isDark ? darkNavInactive : lightNavInactive;
  static Color get formFilledColor => _isDark ? darkNavbar : lightNavbar;

  static Color get navbarColor => _isDark ? darkNavbar : lightNavbar;
  static Color get navActiveColor => primary;
  static Color get navInactiveColor => unSelectedColor;
}
