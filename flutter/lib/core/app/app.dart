// core/app/leave_app.dart
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import '../../config/theme/theme.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/user/presentation/cubit/user_cubit.dart';
import '../../l10n/app_localizations.dart';
import '../layout/main_layout.dart';
import '../locale/locale_cubit.dart';
import '../servicelocator/servicelocator.dart';
import '../theme/theme_cubit.dart';
import '../utiles/app_colors.dart';
import 'splash_screen.dart';

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

                // ✅ always start here
                home: const SplashScreen(),
              );
            },
          );
        },
      ),
    );
  }
}
