import 'package:flutter/material.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../data/models/leave_balance_model.dart';

/// Beautiful gradient card for displaying leave balance
class LeaveBalanceCard extends StatelessWidget {
  final LeaveBalanceModel balance;

  const LeaveBalanceCard({super.key, required this.balance});

  @override
  Widget build(BuildContext context) {
    final config = _getLeaveTypeConfig(balance.type);

    return Container(
      width: 180,
      height: 200,
      margin: const EdgeInsets.only(right: 16),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: config.gradientColors,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: config.gradientColors.first.withValues(alpha: 0.4),
            blurRadius: 20,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Stack(
        children: [
          // Background pattern
          Positioned(
            right: -20,
            top: -20,
            child: _PatternCircle(
              size: 100,
              color: Colors.white.withValues(alpha: 0.1),
            ),
          ),
          Positioned(
            right: 30,
            bottom: -30,
            child: _PatternCircle(
              size: 80,
              color: Colors.white.withValues(alpha: 0.08),
            ),
          ),
          // Icon
          Positioned(
            top: 16,
            left: 16,
            child: Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Icon(
                config.icon,
                color: Colors.white,
                size: 26,
              ),
            ),
          ),
          // Content
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Spacer(),
                // Leave type name
                Text(
                  balance.type,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 8),
                // Remaining days - big number
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      '${balance.remainingDays}',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 42,
                        fontWeight: FontWeight.bold,
                        height: 1,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Padding(
                      padding: const EdgeInsets.only(bottom: 6),
                      child: Text(
                        '/ ${balance.totalDays}',
                        style: TextStyle(
                          color: Colors.white.withValues(alpha: 0.7),
                          fontSize: 14,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  'days remaining',
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.8),
                    fontSize: 12,
                  ),
                ),
                const SizedBox(height: 12),
                // Progress bar
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(
                    value: 1 - balance.usagePercent,
                    backgroundColor: Colors.white.withValues(alpha: 0.2),
                    valueColor: const AlwaysStoppedAnimation(Colors.white),
                    minHeight: 6,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  _LeaveTypeConfig _getLeaveTypeConfig(String type) {
    switch (type.toLowerCase()) {
      case 'annual':
        return _LeaveTypeConfig(
          gradientColors: [const Color(0xFF6366F1), const Color(0xFF818CF8)],
          icon: Icons.beach_access,
        );
      case 'sick':
        return _LeaveTypeConfig(
          gradientColors: [const Color(0xFFEF4444), const Color(0xFFF97316)],
          icon: Icons.local_hospital,
        );
      case 'emergency':
        return _LeaveTypeConfig(
          gradientColors: [const Color(0xFFF59E0B), const Color(0xFFFBBF24)],
          icon: Icons.emergency,
        );
      case 'unpaid':
        return _LeaveTypeConfig(
          gradientColors: [const Color(0xFF64748B), const Color(0xFF94A3B8)],
          icon: Icons.money_off,
        );
      case 'maternity':
        return _LeaveTypeConfig(
          gradientColors: [const Color(0xFFEC4899), const Color(0xFFF472B6)],
          icon: Icons.child_friendly,
        );
      case 'paternity':
        return _LeaveTypeConfig(
          gradientColors: [const Color(0xFF3B82F6), const Color(0xFF60A5FA)],
          icon: Icons.family_restroom,
        );
      default:
        return _LeaveTypeConfig(
          gradientColors: [AppColors.primary, const Color(0xFF9B7BEA)],
          icon: Icons.event_available,
        );
    }
  }
}

class _LeaveTypeConfig {
  final List<Color> gradientColors;
  final IconData icon;

  _LeaveTypeConfig({required this.gradientColors, required this.icon});
}

class _PatternCircle extends StatelessWidget {
  final double size;
  final Color color;

  const _PatternCircle({required this.size, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: color,
      ),
    );
  }
}
