import 'package:flutter/material.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../models/calendar_data_models.dart';

/// Card widget for displaying a leave item in the calendar view
class CalendarLeaveItemCard extends StatelessWidget {
  final CalendarLeaveDto leave;
  final bool showManagerName;

  const CalendarLeaveItemCard({
    super.key,
    required this.leave,
    this.showManagerName = false,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final leaveTypeColor = _getLeaveTypeColor(leave.leaveType);

    return Container(
      decoration: BoxDecoration(
        color: isDark ? AppColors.darkSurface : Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Left color bar
              Container(
                width: 6,
                color: leaveTypeColor,
              ),
              // Content
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Top row: Employee name + Leave type badge
                      Row(
                        children: [
                          // Avatar
                          CircleAvatar(
                            radius: 16,
                            backgroundColor: leaveTypeColor.withValues(alpha: 0.2),
                            child: Text(
                              leave.employeeName.isNotEmpty
                                  ? leave.employeeName[0].toUpperCase()
                                  : 'E',
                              style: TextStyle(
                                color: leaveTypeColor,
                                fontWeight: FontWeight.bold,
                                fontSize: 12,
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          // Employee name
                          Expanded(
                            child: Text(
                              leave.employeeName,
                              style: TextStyle(
                                color: isDark ? AppColors.darkText : AppColors.lightText,
                                fontWeight: FontWeight.w600,
                                fontSize: 14,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          // Leave type badge
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: leaveTypeColor.withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(20),
                              border: Border.all(color: leaveTypeColor, width: 1),
                            ),
                            child: Text(
                              leave.leaveType,
                              style: TextStyle(
                                color: leaveTypeColor,
                                fontSize: 10,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      // Date range
                      Row(
                        children: [
                          Icon(
                            Icons.calendar_today_outlined,
                            size: 14,
                            color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                          ),
                          const SizedBox(width: 6),
                          Text(
                            _formatDateRange(leave.startDate, leave.endDate),
                            style: TextStyle(
                              color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                              fontSize: 12,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: (isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive)
                                  .withValues(alpha: 0.2),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              '${leave.numberOfDays} ${leave.numberOfDays == 1 ? 'day' : 'days'}',
                              style: TextStyle(
                                color: isDark ? AppColors.darkText : AppColors.lightText,
                                fontSize: 10,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _formatDateRange(DateTime start, DateTime end) {
    final months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    
    if (start.year == end.year && start.month == end.month && start.day == end.day) {
      return '${months[start.month - 1]} ${start.day}';
    }
    
    if (start.year == end.year && start.month == end.month) {
      return '${months[start.month - 1]} ${start.day} - ${end.day}';
    }
    
    return '${months[start.month - 1]} ${start.day} - ${months[end.month - 1]} ${end.day}';
  }

  /// Returns color based on leave type
  /// Annual=Purple, Sick=Red, Emergency=Orange, Unpaid=Grey, Maternity=Pink, Paternity=Blue
  Color _getLeaveTypeColor(String leaveType) {
    final type = leaveType.toLowerCase();
    
    if (type.contains('annual')) {
      return AppColors.primary; // Purple
    } else if (type.contains('sick')) {
      return AppColors.error; // Red
    } else if (type.contains('emergency')) {
      return AppColors.warning; // Orange
    } else if (type.contains('unpaid')) {
      return Colors.grey; // Grey
    } else if (type.contains('maternity')) {
      return const Color(0xFFE91E63); // Pink
    } else if (type.contains('paternity')) {
      return const Color(0xFF2196F3); // Blue
    }
    
    return AppColors.primary; // Default to purple
  }
}
