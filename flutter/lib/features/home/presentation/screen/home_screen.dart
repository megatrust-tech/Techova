import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:iconly/iconly.dart';
import '../../../../core/servicelocator/servicelocator.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/services/token_storage_service.dart';
import '../../../notification/presentation/cubit/notifications_cubit.dart';
import '../../../notification/presentation/screen/notifications_view.dart';
import '../../../user/presentation/cubit/user_cubit.dart';
import '../cubit/home_cubit.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  @override
  void initState() {
    super.initState();
    _loadUserData();
  }

  Future<void> _loadUserData() async {
    final tokenStorage = sl<TokenStorageService>();
    final userId = await tokenStorage.getUserId();
    if (userId != null && mounted) {
      context.read<UserCubit>().loadUser(userId);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: PreferredSize(
        preferredSize: const Size.fromHeight(70),
        child: SafeArea(
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                // Logo
                Image.asset(
                  'assets/images/small_taskedin_logo.png',
                  width: 40,
                  height: 40,
                  fit: BoxFit.contain,
                ),

                const SizedBox(width: 12),
                Expanded(
                  child: BlocBuilder<UserCubit, UserState>(
                    builder: (context, state) {
                      String userName = 'User';
                      if (state is UserLoaded) {
                        userName = state.user.firstName;
                      }

                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            l10n.welcomeBack,
                            style: TextStyle(
                              fontSize: 12,
                              color: isDark
                                  ? AppColors.darkText.withValues(alpha: 0.6)
                                  : AppColors.lightText.withValues(alpha: 0.6),
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            userName,
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: isDark ? AppColors.darkText : AppColors.lightText,
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                ),

                // Notification icon
                BlocBuilder<HomeCubit, HomeState>(
                  builder: (context, state) {
                    final cubit = context.read<HomeCubit>();
                    final unread = cubit.unreadCount;

                    return Stack(
                      clipBehavior: Clip.none,
                      children: [
                        Container(
                          decoration: BoxDecoration(
                            color: isDark ? AppColors.darkSurface : AppColors.lightSurface,
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: IconButton(
                            icon: Icon(
                              IconlyLight.notification,
                              color: isDark ? AppColors.darkText : AppColors.lightText,
                            ),
                            onPressed: () async {
                              await Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => BlocProvider<NotificationsCubit>(
                                    create: (_) => sl<NotificationsCubit>(),
                                    child: const NotificationsView(),
                                  ),
                                ),
                              );
                              if (context.mounted) {
                                context.read<HomeCubit>().initialize();
                              }
                            },
                          ),
                        ),

                        if (unread > 0)
                          Positioned(
                            right: -2,
                            top: -2,
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                              decoration: BoxDecoration(
                                color: AppColors.error,
                                borderRadius: BorderRadius.circular(999),
                              ),
                              child: Text(
                                unread > 99 ? '99+' : '$unread',
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 10,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ),
                      ],
                    );
                  },
                ),
              ],
            ),
          ),
        ),
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.dashboard_outlined, size: 80, color: AppColors.primary),
            const SizedBox(height: 16),
            Text(
              l10n.homeDashboard,
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              l10n.comingSoon,
              style: const TextStyle(color: Colors.grey),
            ),
          ],
        ),
      ),
    );
  }
}
