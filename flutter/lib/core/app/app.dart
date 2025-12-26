import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import '../../config/theme/theme.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/auth/presentation/screen/login_screen.dart';
import '../../features/user/presentation/cubit/user_cubit.dart';
import '../../l10n/app_localizations.dart';
import '../layout/main_layout.dart';
import '../locale/locale_cubit.dart';
import '../servicelocator/servicelocator.dart';
import '../theme/theme_cubit.dart';

// ✅ ADD THIS IMPORT (adjust path to your AppColors file)
import '../utiles/app_colors.dart';

class LeaveApp extends StatelessWidget {
  const LeaveApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<AuthBloc>(
          create: (_) => sl<AuthBloc>()..add(const AppStarted()),
        ),
        BlocProvider<UserCubit>(
          create: (_) => sl<UserCubit>(),
        ),
        BlocProvider<ThemeCubit>(
          create: (_) => sl<ThemeCubit>(),
        ),
        BlocProvider<LocaleCubit>(
          create: (_) => sl<LocaleCubit>(),
        ),
      ],
      child: BlocBuilder<ThemeCubit, ThemeMode>(
        builder: (context, themeMode) {
          AppColors.setThemeMode(themeMode);

          return BlocBuilder<LocaleCubit, Locale>(
            builder: (context, locale) {
              return MaterialApp(
                title: 'TaskedIn',
                debugShowCheckedModeBanner: false,

                theme: AppTheme.lightTheme,
                darkTheme: AppTheme.darkTheme,
                themeMode: themeMode,

                localizationsDelegates: const [
                  AppLocalizations.delegate,
                  GlobalMaterialLocalizations.delegate,
                  GlobalWidgetsLocalizations.delegate,
                  GlobalCupertinoLocalizations.delegate,
                ],
                supportedLocales: const [
                  Locale('en'),
                  Locale('ar'),
                ],
                locale: locale,

                home: BlocBuilder<AuthBloc, AuthState>(
                  builder: (context, state) {
                    return switch (state) {
                      AuthInitial() => _buildLoadingScreen(context),
                      AuthLoading() => _buildLoadingScreen(context),
                      LoginLoading() => const LoginScreen(), // Stay on login screen
                      Authenticated() => const MainLayout(),
                      Unauthenticated() => const LoginScreen(),
                      AuthFailure() => const LoginScreen(),
                    };
                  },
                ),
              );
            },
          );
        },
      ),
    );
  }

  Widget _buildLoadingScreen(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const CircularProgressIndicator(),
            const SizedBox(height: 16),
            Text(l10n?.loading ?? 'Loading...'),
          ],
        ),
      ),
    );
  }
}