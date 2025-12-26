// core/themeing/theme_cubit.dart
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../utiles/app_colors.dart';


class ThemeCubit extends Cubit<ThemeMode> {
  static const String _themeKey = 'app_theme_mode';

  ThemeCubit() : super(ThemeMode.system) {
    AppColors.setThemeMode(state);
    _loadTheme();
  }

  /// Load saved theme from SharedPreferences
  Future<void> _loadTheme() async {
    final prefs = await SharedPreferences.getInstance();
    final themeIndex = prefs.getInt(_themeKey);

    if (themeIndex != null) {
      final mode = ThemeMode.values[themeIndex];
      AppColors.setThemeMode(mode);
      emit(mode);
    }
  }

  /// Toggle between light and dark mode
  Future<void> toggleTheme() async {
    final newMode = state == ThemeMode.dark ? ThemeMode.light : ThemeMode.dark;
    await _saveTheme(newMode);
    AppColors.setThemeMode(newMode);
    emit(newMode);
  }

  /// Set specific theme mode
  Future<void> setTheme(ThemeMode mode) async {
    await _saveTheme(mode);
    AppColors.setThemeMode(mode);
    emit(mode);
  }

  /// Check if dark mode is active
  bool get isDarkMode => state == ThemeMode.dark;

  /// Save theme to SharedPreferences
  Future<void> _saveTheme(ThemeMode mode) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt(_themeKey, mode.index);
  }
}
