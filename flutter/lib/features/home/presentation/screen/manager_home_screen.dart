import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:iconly/iconly.dart';

import '../../../../core/servicelocator/servicelocator.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/services/token_storage_service.dart';
import '../../../leaves/presentation/screen/employee_leaves_screen.dart';
import '../../../leaves/presentation/screen/leave_details_screen.dart';
import '../../../notification/presentation/cubit/notifications_cubit.dart';
import '../../../notification/presentation/screen/notifications_view.dart';
import '../../../user/presentation/cubit/user_cubit.dart';
import '../../data/repositories/home_repository.dart';
import '../cubit/home_cubit.dart';
import '../widgets/leave_balance_card.dart';
import '../widgets/recent_leave_card.dart';

class ManagerHomeScreen extends StatefulWidget {
  const ManagerHomeScreen({super.key});

  @override
  State<ManagerHomeScreen> createState() => _ManagerHomeScreenState();
}

class _ManagerHomeScreenState extends State<ManagerHomeScreen> {
  @override
  void initState() {
    super.initState();
    _loadUserData();
    // Initialize home data on first load
    sl<HomeCubit>().initialize();
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

    return BlocProvider.value(
      value: sl<HomeCubit>(),
      child: Scaffold(
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

                  // Welcome text and user name
                  Expanded(
                    child: BlocBuilder<UserCubit, UserState>(
                      builder: (context, state) {
                        String userName = 'Manager';
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
        body: BlocBuilder<HomeCubit, HomeState>(
          builder: (context, state) {
            if (state is HomeInitial) {
              return const Center(child: CircularProgressIndicator());
            }

            if (state is! HomeLoaded) {
              return const Center(child: CircularProgressIndicator());
            }

            return RefreshIndicator(
              onRefresh: () => context.read<HomeCubit>().refresh(),
              child: SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Manager/HR badge
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 20),
                      child: _buildRoleBadge(),
                    ),
                    const SizedBox(height: 20),
                    // Leave Balance Section
                    _buildLeaveBalanceSection(context, state, isDark, l10n),
                    const SizedBox(height: 32),
                    // Recent Leaves Section  
                    _buildRecentLeavesSection(context, state, isDark, l10n),
                    const SizedBox(height: 32),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildRoleBadge() {
    return BlocBuilder<UserCubit, UserState>(
      builder: (context, state) {
        String roleLabel = 'Manager / HR View';
        if (state is UserLoaded) {
          final roleName = state.user.role.name;
          roleLabel = '$roleName View';
        }

        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: AppColors.warning.withValues(alpha: 0.15),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.admin_panel_settings_outlined, size: 16, color: AppColors.warning),
              const SizedBox(width: 6),
              Text(
                roleLabel,
                style: TextStyle(
                  color: AppColors.warning,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildLeaveBalanceSection(
    BuildContext context,
    HomeLoaded state,
    bool isDark,
    AppLocalizations l10n,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Section header
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20),
          child: Text(
            l10n.leaveBalance,
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: isDark ? AppColors.darkText : AppColors.lightText,
            ),
          ),
        ),
        const SizedBox(height: 16),
        // Cards
        if (state.isLoadingBalance)
          const SizedBox(
            height: 200,
            child: Center(child: CircularProgressIndicator()),
          )
        else if (state.leaveBalance.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Container(
              height: 200,
              decoration: BoxDecoration(
                color: isDark ? Colors.grey.shade900 : Colors.grey.shade100,
                borderRadius: BorderRadius.circular(24),
              ),
              child: Center(
                child: Text(
                  'No leave balance data',
                  style: TextStyle(
                    color: Colors.grey.shade600,
                  ),
                ),
              ),
            ),
          )
        else
          SizedBox(
            height: 200,
            child: ListView.builder(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 20),
              itemCount: state.leaveBalance.length,
              itemBuilder: (context, index) {
                return LeaveBalanceCard(balance: state.leaveBalance[index]);
              },
            ),
          ),
      ],
    );
  }

  Widget _buildRecentLeavesSection(
    BuildContext context,
    HomeLoaded state,
    bool isDark,
    AppLocalizations l10n,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Section header with See All
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                l10n.myLeaves,
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: isDark ? AppColors.darkText : AppColors.lightText,
                ),
              ),
              TextButton(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (context) => const EmployeeLeavesScreen(),
                    ),
                  );
                },
                child: Text(
                  l10n.seeAll,
                  style: TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        // Recent leaves list
        if (state.isLoadingLeaves)
          const SizedBox(
            height: 100,
            child: Center(child: CircularProgressIndicator()),
          )
        else if (state.recentLeaves.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Container(
              height: 100,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      Icons.event_available,
                      size: 32,
                      color: Colors.grey.shade500,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'No recent leave requests',
                      style: TextStyle(
                        color: Colors.grey.shade600,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          )
        else
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Column(
              children: state.recentLeaves.map((leave) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: BlocBuilder<UserCubit, UserState>(
                    builder: (context, userState) {
                      String? userName;
                      if (userState is UserLoaded) {
                        userName = userState.user.fullName;
                      }
                      return RecentLeaveCard(
                        leave: leave,
                        onTap: () async {
                          final result = await Navigator.of(context).push<bool>(
                            MaterialPageRoute(
                              builder: (context) => LeaveDetailsScreen(
                                leave: leave,
                                displayName: userName ?? 'User',
                              ),
                            ),
                          );
                          if (result == true && context.mounted) {
                            context.read<HomeCubit>().refresh();
                          }
                        },
                      );
                    },
                  ),
                );
              }).toList(),
            ),
          ),
      ],
    );
  }
}
