import 'package:flutter/material.dart';
import 'package:iconly/iconly.dart';

import '../../features/calender/presentation/calender_view.dart';
import '../../l10n/app_localizations.dart';
import '../utiles/app_colors.dart';
import '../../features/home/presentation/screen/role_based_home.dart';
import '../../features/leave_request/presentation/screen/new_leave_request_screen.dart';
import '../../features/leaves/presentation/screen/role_based_leaves_screen.dart';
import '../../features/profile/presentation/screen/profile_screen.dart';

class MainLayout extends StatefulWidget {
  const MainLayout({super.key});

  @override
  State<MainLayout> createState() => _MainLayoutState();
}

class _MainLayoutState extends State<MainLayout> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final l10n = AppLocalizations.of(context)!;

    final screens = [
      const RoleBasedHome(),
      const RoleBasedLeavesScreen(),
      const SizedBox.shrink(), // center add button
      const CalendarView(),
      const ProfileScreen(),
    ];


    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: screens,
      ),
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          color: isDark ? AppColors.darkNavbar : AppColors.lightNavbar,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.1),
              blurRadius: 10,
              offset: const Offset(0, -2),
            ),
          ],
        ),
        child: SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(
                  index: 0,
                  icon: IconlyLight.home,
                  activeIcon: IconlyBold.home,
                  label: l10n.home,
                  isDark: isDark,
                ),
                _buildNavItem(
                  index: 1,
                  icon: IconlyLight.logout,
                  activeIcon: IconlyBold.logout,
                  label: l10n.leaves,
                  isDark: isDark,
                ),
                _buildCenterButton(isDark),
                _buildNavItem(
                  index: 3,
                  icon: IconlyLight.calendar,
                  activeIcon: IconlyBold.calendar,
                  label: l10n.calendar,
                  isDark: isDark,
                ),
                _buildNavItem(
                  index: 4,
                  icon: IconlyLight.profile,
                  activeIcon: IconlyBold.profile,
                  label: l10n.profile,
                  isDark: isDark,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  void _onItemTapped(int index) {
    setState(() {
      _currentIndex = index;
    });
  }

  Widget _buildNavItem({
    required int index,
    required IconData icon,
    required IconData activeIcon,
    required String label,
    required bool isDark,
  }) {
    final isSelected = _currentIndex == index;
    final activeColor = isDark ? AppColors.darkNavActive : AppColors.lightNavActive;
    final inactiveColor = isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive;
    final color = isSelected ? activeColor : inactiveColor;

    return InkWell(
      onTap: () => _onItemTapped(index),
      borderRadius: BorderRadius.circular(16),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isSelected ? activeIcon : icon,
              color: color,
              size: 24,
            ),
            const SizedBox(height: 4),
            Text(
              label,
              style: TextStyle(
                color: color,
                fontSize: 11,
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCenterButton(bool isDark) {
    return GestureDetector(
      onTap: () {
        Navigator.of(context).push(
          MaterialPageRoute(
            builder: (context) => const NewLeaveRequestScreen(),
          ),
        );
      },
      child: Container(
        width: 56,
        height: 56,
        decoration: BoxDecoration(
          gradient: LinearGradient(
            colors: [
              AppColors.primary,
              const Color(0xFF9B7BEA),
            ],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(
              color: AppColors.primary.withValues(alpha: 0.4),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: const Icon(
          Icons.add,
          color: Colors.white,
          size: 28,
        ),
      ),
    );
  }
}

class _PlaceholderScreen extends StatelessWidget {
  final String title;

  const _PlaceholderScreen({required this.title});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: Center(
        child: Text(
          '$title\n${l10n.comingSoon}',
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.headlineSmall,
        ),
      ),
    );
  }
}
