import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../../leaves/data/models/leave_item_model.dart';

/// Compact leave card for recent leaves section
class RecentLeaveCard extends StatelessWidget {
  final LeaveItem leave;
  final VoidCallback? onTap;

  const RecentLeaveCard({
    super.key,
    required this.leave,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final config = _getLeaveTypeConfig(leave.leaveType);
    final dateFormat = DateFormat('MMM dd');

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF232337) : Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isDark
                ? Colors.grey.withValues(alpha: 0.2)
                : Colors.grey.withValues(alpha: 0.15),
          ),
        ),
        child: Row(
          children: [
            // Leave type icon with gradient background
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: config.gradientColors,
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(
                config.icon,
                color: Colors.white,
                size: 24,
              ),
            ),
            const SizedBox(width: 12),
            // Leave details
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        leave.leaveType,
                        style: TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                          color: isDark ? Colors.white : Colors.black87,
                        ),
                      ),
                      const Spacer(),
                      _StatusBadge(status: leave.status),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${dateFormat.format(leave.startDate)} - ${dateFormat.format(leave.endDate)}',
                    style: TextStyle(
                      fontSize: 13,
                      color: isDark ? Colors.grey.shade400 : Colors.grey.shade600,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            // Days count
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: config.gradientColors.first.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Column(
                children: [
                  Text(
                    '${leave.numberOfDays}',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: config.gradientColors.first,
                    ),
                  ),
                  Text(
                    leave.numberOfDays == 1 ? 'Day' : 'Days',
                    style: TextStyle(
                      fontSize: 10,
                      color: config.gradientColors.first.withValues(alpha: 0.8),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
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

class _StatusBadge extends StatelessWidget {
  final String status;

  const _StatusBadge({required this.status});

  @override
  Widget build(BuildContext context) {
    final statusColor = _getStatusColor(status);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: statusColor.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        _formatStatus(status),
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.w600,
          color: statusColor,
        ),
      ),
    );
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'approved':
        return AppColors.success;
      case 'rejected':
      case 'cancelled':
        return AppColors.error;
      case 'pendingmanager':
      case 'pendinghr':
      case 'pending':
        return AppColors.warning;
      default:
        return Colors.grey;
    }
  }

  String _formatStatus(String status) {
    return status.replaceAllMapped(
      RegExp(r'([a-z])([A-Z])'),
      (match) => '${match.group(1)} ${match.group(2)}',
    );
  }
}
