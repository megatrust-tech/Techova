import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/locale/locale_cubit.dart';
import '../../../../core/theme/theme_cubit.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/screen/login_screen.dart';
import '../../../user/presentation/cubit/user_cubit.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final l10n = AppLocalizations.of(context)!;

    return BlocListener<AuthBloc, AuthState>(
      listenWhen: (prev, curr) => curr is Unauthenticated,
      listener: (context, state) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(builder: (_) => const LoginScreen()),
              (_) => false,
        );
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.profile),
          centerTitle: true,
        ),
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              _buildUserCard(context, isDark),
              const SizedBox(height: 24),
              _buildSettingsSection(context, isDark, l10n),
              const SizedBox(height: 24),
              _buildLogoutButton(context, l10n),
            ],
          ),
        ),
      ),
    );
  }


  Widget _buildUserCard(BuildContext context, bool isDark) {
    return BlocBuilder<UserCubit, UserState>(
      builder: (context, state) {
        String firstName = '';
        String lastName = '';
        String email = '';
        String role = '';

        if (state is UserLoaded) {
          firstName = state.user.firstName;
          lastName = state.user.lastName;
          email = state.user.email;
          role = state.user.role.name;
        }

        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: isDark ? AppColors.darkSurface : Colors.white,
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.05),
                blurRadius: 10,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            children: [
              // Avatar
              Container(
                width: 80,
                height: 80,
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [
                      AppColors.primary,
                      AppColors.primary.withValues(alpha: 0.7),
                    ],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                  shape: BoxShape.circle,
                ),
                child: Center(
                  child: Text(
                    firstName.isNotEmpty ? firstName[0].toUpperCase() : '?',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 32,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                '$firstName $lastName',
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
              ),
              const SizedBox(height: 4),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  role,
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              const SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.email_outlined,
                    size: 16,
                    color: isDark
                        ? AppColors.darkText.withValues(alpha: 0.6)
                        : AppColors.lightText.withValues(alpha: 0.6),
                  ),
                  const SizedBox(width: 6),
                  Text(
                    email,
                    style: TextStyle(
                      color: isDark
                          ? AppColors.darkText.withValues(alpha: 0.6)
                          : AppColors.lightText.withValues(alpha: 0.6),
                      fontSize: 14,
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildSettingsSection(BuildContext context, bool isDark, AppLocalizations l10n) {
    final isArabic = context.watch<LocaleCubit>().isArabic;
    
    return Container(
      decoration: BoxDecoration(
        color: isDark ? AppColors.darkSurface : Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          // Theme Toggle
          _buildSettingsTile(
            context: context,
            icon: isDark ? Icons.dark_mode : Icons.light_mode,
            title: l10n.theme,
            subtitle: isDark ? l10n.darkMode : l10n.lightMode,
            trailing: Switch(
              value: isDark,
              activeTrackColor: AppColors.primary.withValues(alpha: 0.5),
              activeColor: AppColors.primary,
              onChanged: (value) {
                context.read<ThemeCubit>().toggleTheme();
              },
            ),
            isDark: isDark,
          ),
          // Divider(
          //   height: 1,
          //   indent: 60,
          //   color: isDark ? Colors.white12 : Colors.black12,
          // ),
          // // Language Toggle
          // _buildSettingsTile(
          //   context: context,
          //   icon: Icons.language,
          //   title: l10n.language,
          //   subtitle: isArabic ? l10n.arabic : l10n.english,
          //   trailing: const Icon(Icons.chevron_right),
          //   onTap: () => _showLanguageDialog(context, l10n),
          //   isDark: isDark,
          // ),
        ],
      ),
    );
  }

  Widget _buildSettingsTile({
    required BuildContext context,
    required IconData icon,
    required String title,
    required String subtitle,
    required Widget trailing,
    required bool isDark,
    VoidCallback? onTap,
  }) {
    return ListTile(
      onTap: onTap,
      leading: Container(
        width: 40,
        height: 40,
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Icon(icon, color: AppColors.primary),
      ),
      title: Text(
        title,
        style: const TextStyle(fontWeight: FontWeight.w600),
      ),
      subtitle: Text(
        subtitle,
        style: TextStyle(
          color: isDark
              ? AppColors.darkText.withValues(alpha: 0.5)
              : AppColors.lightText.withValues(alpha: 0.5),
          fontSize: 12,
        ),
      ),
      trailing: trailing,
    );
  }

  void _showLanguageDialog(BuildContext context, AppLocalizations l10n) {
    showDialog(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.selectLanguage),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Text('🇺🇸', style: TextStyle(fontSize: 24)),
              title: Text(l10n.english),
              onTap: () {
                Navigator.pop(dialogContext);
                context.read<LocaleCubit>().setLocale(const Locale('en'));
              },
            ),
            ListTile(
              leading: const Text('🇸🇦', style: TextStyle(fontSize: 24)),
              title: Text(l10n.arabic),
              onTap: () {
                Navigator.pop(dialogContext);
                context.read<LocaleCubit>().setLocale(const Locale('ar'));
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLogoutButton(BuildContext context, AppLocalizations l10n) {
    return BlocBuilder<AuthBloc, AuthState>(
      builder: (context, state) {
        final isLoading = state is LogoutLoading;

        return SizedBox(
          width: double.infinity,
          child: OutlinedButton(
            onPressed: isLoading ? null : () => _showLogoutConfirmation(context, l10n),
            style: OutlinedButton.styleFrom(
              foregroundColor: AppColors.error,
              side: const BorderSide(color: AppColors.error),
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
              ),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (isLoading) ...[
                  const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                  const SizedBox(width: 10),
                ] else ...[
                  const Icon(Icons.logout),
                  const SizedBox(width: 10),
                ],
                Text(
                  isLoading ? l10n.loading : l10n.logout,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _showLogoutConfirmation(BuildContext context, AppLocalizations l10n) {
    showDialog(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(l10n.logout),
        content: Text(l10n.logoutConfirmation),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: Text(l10n.cancel),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(dialogContext);
              context.read<AuthBloc>().add(const LogoutRequested());
            },
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            child: Text(l10n.logout),
          ),
        ],
      ),
    );
  }
}
