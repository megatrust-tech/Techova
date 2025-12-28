import 'package:flutter/material.dart';
import 'package:timeline_tile/timeline_tile.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../data/models/leave_audit_log_model.dart';

/// Widget that displays leave request history as a timeline
class LeaveHistoryTimeline extends StatelessWidget {
  final List<LeaveAuditLogDto> history;

  const LeaveHistoryTimeline({
    super.key,
    required this.history,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    if (history.isEmpty) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isDark ? AppColors.darkSurface : Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: Colors.grey.withValues(alpha: 0.3),
          ),
        ),
        child: Row(
          children: [
            Icon(Icons.history, color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive),
            const SizedBox(width: 12),
            Text(
              'No history available',
              style: TextStyle(
                color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                fontSize: 14,
              ),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: history.length,
      itemBuilder: (context, index) {
        final log = history[index];
        final isFirst = index == 0;
        final isLast = index == history.length - 1;

        return TimelineTile(
          isFirst: isFirst,
          isLast: isLast,
          indicatorStyle: IndicatorStyle(
            width: 32,
            height: 32,
            indicator: _buildIndicator(log.action, isDark),
          ),
          beforeLineStyle: LineStyle(
            color: AppColors.primary.withValues(alpha: 0.3),
            thickness: 2,
          ),
          afterLineStyle: LineStyle(
            color: AppColors.primary.withValues(alpha: 0.3),
            thickness: 2,
          ),
          endChild: Padding(
            padding: const EdgeInsets.only(left: 12, bottom: 16, top: 4),
            child: _buildTimelineCard(context, log, isDark),
          ),
        );
      },
    );
  }

  Widget _buildIndicator(String action, bool isDark) {
    final color = _getActionColor(action);
    final icon = _getActionIcon(action);

    return Container(
      width: 32,
      height: 32,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        shape: BoxShape.circle,
        border: Border.all(color: color, width: 2),
      ),
      child: Icon(
        icon,
        size: 16,
        color: color,
      ),
    );
  }

  Widget _buildTimelineCard(BuildContext context, LeaveAuditLogDto log, bool isDark) {
    final actionColor = _getActionColor(log.action);

    return Container(
      padding: const EdgeInsets.all(12),
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
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Action name with icon
          Row(
            children: [
              Icon(
                _getActionIcon(log.action),
                size: 18,
                color: actionColor,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  _getActionDisplayName(log.action),
                  style: TextStyle(
                    color: isDark ? AppColors.darkText : AppColors.lightText,
                    fontWeight: FontWeight.w700,
                    fontSize: 14,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          // Actor name
          Text(
            'by ${log.actionBy}',
            style: TextStyle(
              color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
              fontSize: 12,
            ),
          ),
          const SizedBox(height: 4),
          // Date/time
          Text(
            _formatDate(log.actionDate),
            style: TextStyle(
              color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
              fontSize: 11,
            ),
          ),
          // Comment (if exists)
          if (log.comment != null && log.comment!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: actionColor.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(8),
                border: Border(
                  left: BorderSide(color: actionColor, width: 3),
                ),
              ),
              child: Text(
                '"${log.comment}"',
                style: TextStyle(
                  color: isDark ? AppColors.darkText : AppColors.lightText,
                  fontSize: 12,
                  fontStyle: FontStyle.italic,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final difference = now.difference(date);

    // If less than 24 hours, show relative time
    if (difference.inHours < 24) {
      if (difference.inMinutes < 1) {
        return 'Just now';
      } else if (difference.inMinutes < 60) {
        return '${difference.inMinutes} ${difference.inMinutes == 1 ? 'minute' : 'minutes'} ago';
      } else {
        return '${difference.inHours} ${difference.inHours == 1 ? 'hour' : 'hours'} ago';
      }
    }

    // Otherwise, show formatted date
    final months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final hour = date.hour > 12 ? date.hour - 12 : (date.hour == 0 ? 12 : date.hour);
    final amPm = date.hour >= 12 ? 'PM' : 'AM';
    final minute = date.minute.toString().padLeft(2, '0');
    
    return '${months[date.month - 1]} ${date.day}, ${date.year} • $hour:$minute $amPm';
  }

  Color _getActionColor(String action) {
    final actionLower = action.toLowerCase();
    
    if (actionLower.contains('submitted')) {
      return const Color(0xFF2196F3); // Blue
    } else if (actionLower.contains('approved')) {
      return AppColors.success; // Green
    } else if (actionLower.contains('rejected')) {
      return AppColors.error; // Red
    } else if (actionLower.contains('cancelled') || actionLower.contains('canceled')) {
      return Colors.grey;
    }
    
    return AppColors.primary; // Default purple
  }

  IconData _getActionIcon(String action) {
    final actionLower = action.toLowerCase();
    
    if (actionLower.contains('submitted')) {
      return Icons.send;
    } else if (actionLower.contains('managerapproved')) {
      return Icons.check_circle;
    } else if (actionLower.contains('managerrejected')) {
      return Icons.cancel;
    } else if (actionLower.contains('hrapproved')) {
      return Icons.verified;
    } else if (actionLower.contains('hrrejected')) {
      return Icons.cancel;
    } else if (actionLower.contains('cancelled') || actionLower.contains('canceled')) {
      return Icons.close;
    }
    
    return Icons.info;
  }

  String _getActionDisplayName(String action) {
    switch (action.toLowerCase()) {
      case 'submitted':
        return 'Request Submitted';
      case 'managerapproved':
        return 'Manager Approved';
      case 'managerrejected':
        return 'Manager Rejected';
      case 'hrapproved':
        return 'HR Approved';
      case 'hrrejected':
        return 'HR Rejected';
      case 'cancelled':
      case 'canceled':
        return 'Cancelled';
      default:
        return action;
    }
  }
}
