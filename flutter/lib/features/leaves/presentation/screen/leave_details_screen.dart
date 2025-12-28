import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/servicelocator/servicelocator.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/models/leave_item_model.dart';
import '../../data/models/leave_audit_log_model.dart';
import '../../data/data_sources/leaves_data_source.dart';
import '../widgets/leave_history_timeline.dart';

/// Employee leave details screen with read-only fields and cancel option
class LeaveDetailsScreen extends StatefulWidget {
  final LeaveItem leave;
  final String displayName;

  const LeaveDetailsScreen({
    super.key,
    required this.leave,
    required this.displayName,
  });

  @override
  State<LeaveDetailsScreen> createState() => _LeaveDetailsScreenState();
}

class _LeaveDetailsScreenState extends State<LeaveDetailsScreen> {
  bool _isCancelling = false;
  bool _isLoadingHistory = true;
  List<LeaveAuditLogDto> _history = [];
  String? _historyError;
  bool _isDownloadingAttachment = false;

  bool get _canCancel {
    final status = widget.leave.status.toLowerCase();
    return status == 'pendingmanager' || status == 'pendinghr';
  }

  @override
  void initState() {
    super.initState();
    _loadHistory();
  }

  Future<void> _loadHistory() async {
    setState(() {
      _isLoadingHistory = true;
      _historyError = null;
    });

    try {
      final dataSource = sl<LeavesDataSource>();
      final history = await dataSource.getRequestHistory(widget.leave.id);
      if (mounted) {
        setState(() {
          _history = history;
          _isLoadingHistory = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _historyError = 'Failed to load history';
          _isLoadingHistory = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final l10n = AppLocalizations.of(context)!;

    // Shared decoration matching NewLeaveRequestScreen
    final baseInputDecoration = InputDecoration(
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide(
          color: Colors.grey.withValues(alpha: 0.3),
        ),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide(
          color: AppColors.primary,
          width: 1.5,
        ),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
    );

    return Scaffold(
      appBar: AppBar(
        title: const Text('Leave Details'),
        elevation: 0,
        backgroundColor: theme.scaffoldBackgroundColor,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Status badge at top
            _buildStatusBadge(isDark),
            const SizedBox(height: 24),

            // Leave Type
            _SectionHeader(title: l10n.reason),
            const SizedBox(height: 12),
            InputDecorator(
              decoration: baseInputDecoration,
              child: _ReadOnlyContent(
                value: widget.leave.leaveType,
                icon: Icons.category,
                isDark: isDark,
              ),
            ),
            const SizedBox(height: 24),

            // Duration
            _SectionHeader(title: l10n.startDate),
            const SizedBox(height: 12),
            InputDecorator(
              decoration: baseInputDecoration,
              child: _buildDateRangeContent(isDark),
            ),
            const SizedBox(height: 24),

            // Notes
            if (widget.leave.notes != null && widget.leave.notes!.isNotEmpty) ...[
              _SectionHeader(title: l10n.reason, subtitle: 'Notes'),
              const SizedBox(height: 12),
              InputDecorator(
                decoration: baseInputDecoration,
                child: _ReadOnlyContent(
                  value: widget.leave.notes!,
                  icon: Icons.notes,
                  isDark: isDark,
                  multiline: true,
                ),
              ),
              const SizedBox(height: 24),
            ],

            // Attachment
            if (widget.leave.attachmentUrl != null &&
                widget.leave.attachmentUrl!.isNotEmpty) ...[
              const _SectionHeader(title: 'Attachment'),
              const SizedBox(height: 12),
              InputDecorator(
                decoration: baseInputDecoration,
                child: _buildAttachmentContent(isDark),
              ),
              const SizedBox(height: 24),
            ],

            // Cancel button (only for pending statuses)
            if (_canCancel) ...[
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _isCancelling ? null : _showCancelConfirmation,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.error,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  child: _isCancelling
                      ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                      : const Text('Cancel Request'),
                ),
              ),
            ],
            const SizedBox(height: 32),

            // Request History Timeline
            _SectionHeader(title: 'Request History'),
            const SizedBox(height: 12),
            _buildHistorySection(isDark),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildHistorySection(bool isDark) {
    if (_isLoadingHistory) {
      return Container(
        padding: const EdgeInsets.all(24),
        child: Center(
          child: CircularProgressIndicator(color: AppColors.primary),
        ),
      );
    }

    if (_historyError != null) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isDark ? AppColors.darkSurface : Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: AppColors.error.withValues(alpha: 0.3),
          ),
        ),
        child: Row(
          children: [
            Icon(Icons.error_outline, color: AppColors.error),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                _historyError!,
                style: TextStyle(
                  color: isDark ? AppColors.darkText : AppColors.lightText,
                  fontSize: 14,
                ),
              ),
            ),
            TextButton(
              onPressed: _loadHistory,
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    return LeaveHistoryTimeline(history: _history);
  }

  Widget _buildStatusBadge(bool isDark) {
    final statusColor = _getStatusColor(widget.leave.status);
    final formattedStatus = _formatStatus(widget.leave.status);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: statusColor.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: statusColor.withValues(alpha: 0.5),
        ),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            _getStatusIcon(widget.leave.status),
            color: statusColor,
            size: 20,
          ),
          const SizedBox(width: 8),
          Text(
            formattedStatus,
            style: TextStyle(
              color: statusColor,
              fontWeight: FontWeight.w600,
              fontSize: 16,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDateRangeContent(bool isDark) {
    final dateFormat = DateFormat('MMM dd, yyyy');
    final startStr = dateFormat.format(widget.leave.startDate);
    final endStr = dateFormat.format(widget.leave.endDate);

    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _DateCard(label: 'From', date: startStr, isDark: isDark),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: Icon(
                Icons.arrow_forward,
                color: AppColors.primary,
                size: 20,
              ),
            ),
            Expanded(
              child: _DateCard(label: 'To', date: endStr, isDark: isDark),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: AppColors.primary.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(16),
          ),
          child: Text(
            '${widget.leave.numberOfDays} ${widget.leave.numberOfDays == 1 ? 'day' : 'days'}',
            style: TextStyle(
              color: AppColors.primary,
              fontWeight: FontWeight.w600,
              fontSize: 12,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildAttachmentContent(bool isDark) {
    return InkWell(
      onTap: _isDownloadingAttachment ? null : _downloadAttachment,
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: _isDownloadingAttachment
                ? Padding(
                    padding: const EdgeInsets.all(10),
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppColors.primary,
                    ),
                  )
                : Icon(
                    Icons.attach_file,
                    color: AppColors.primary,
                    size: 20,
                  ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _isDownloadingAttachment ? 'Downloading...' : 'View Attachment',
                  style: TextStyle(
                    fontWeight: FontWeight.w500,
                    color: isDark ? Colors.white : Colors.black87,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  _isDownloadingAttachment ? 'Please wait' : 'Tap to download',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey.shade600,
                  ),
                ),
              ],
            ),
          ),
          if (!_isDownloadingAttachment)
            Icon(
              Icons.download,
              color: AppColors.primary,
              size: 20,
            ),
        ],
      ),
    );
  }

  Future<void> _downloadAttachment() async {
    if (_isDownloadingAttachment) return;

    setState(() => _isDownloadingAttachment = true);

    try {
      final dioClient = sl<DioClient>();
      
      // Use the endpoint: GET /leaves/{id}/attachment
      final response = await dioClient.get(
        '/leaves/${widget.leave.id}/attachment',
        options: Options(responseType: ResponseType.bytes),
      );

      // Get filename from Content-Disposition header
      String filename = 'attachment_${widget.leave.id}';
      final contentDisposition = response.headers['content-disposition'];
      if (contentDisposition != null && contentDisposition.isNotEmpty) {
        final header = contentDisposition.first;
        if (header.contains('filename=')) {
          filename = header.split('filename=').last.replaceAll('"', '').replaceAll("'", '');
        }
      }

      // Get extension from Content-Type if not in filename
      if (!filename.contains('.')) {
        final contentType = response.headers['content-type']?.first ?? '';
        final ext = _getExtensionFromContentType(contentType);
        if (ext.isNotEmpty) {
          filename = '$filename.$ext';
        }
      }

      // Save to temporary directory
      final tempDir = await getTemporaryDirectory();
      final filePath = '${tempDir.path}/$filename';
      final file = File(filePath);
      await file.writeAsBytes(response.data);

      // Share/open the file
      await Share.shareXFiles(
        [XFile(filePath)],
        subject: 'Leave Attachment',
      );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Attachment downloaded: $filename'),
            backgroundColor: AppColors.success,
          ),
        );
      }
    } on DioException catch (e) {
      if (mounted) {
        String message = 'Failed to download attachment';
        if (e.response?.statusCode == 404) {
          message = 'No attachment found for this request';
        } else if (e.response?.statusCode == 403) {
          message = 'Not authorized to view attachment';
        } else if (e.response?.data is Map<String, dynamic>) {
          final data = e.response!.data as Map<String, dynamic>;
          message = data['message']?.toString() ?? message;
        }
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(message),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error: ${e.toString()}'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isDownloadingAttachment = false);
      }
    }
  }

  String _getExtensionFromContentType(String contentType) {
    final type = contentType.toLowerCase();
    if (type.contains('pdf')) return 'pdf';
    if (type.contains('jpeg') || type.contains('jpg')) return 'jpg';
    if (type.contains('png')) return 'png';
    if (type.contains('gif')) return 'gif';
    if (type.contains('msword')) return 'doc';
    if (type.contains('wordprocessingml')) return 'docx';
    if (type.contains('spreadsheetml')) return 'xlsx';
    if (type.contains('ms-excel')) return 'xls';
    return '';
  }

  void _showCancelConfirmation() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel Leave Request'),
        content: const Text(
          'Are you sure you want to cancel this leave request? This action cannot be undone.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('No, Keep It'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.of(context).pop();
              _cancelLeaveRequest();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
            ),
            child: const Text('Yes, Cancel'),
          ),
        ],
      ),
    );
  }

  Future<void> _cancelLeaveRequest() async {
    setState(() => _isCancelling = true);

    try {
      final dioClient = sl<DioClient>();
      await dioClient.post(ApiEndpoints.cancelLeave(widget.leave.id));

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('Leave request cancelled successfully'),
            backgroundColor: AppColors.success,
          ),
        );
        Navigator.of(context).pop(true); // Return true to indicate refresh needed
      }
    } on DioException catch (e) {
      if (mounted) {
        String message = 'Failed to cancel leave request';
        if (e.response?.data is Map<String, dynamic>) {
          final data = e.response!.data as Map<String, dynamic>;
          message = data['message']?.toString() ?? message;
        }
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(message),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isCancelling = false);
      }
    }
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

  IconData _getStatusIcon(String status) {
    switch (status.toLowerCase()) {
      case 'approved':
        return Icons.check_circle;
      case 'rejected':
        return Icons.cancel;
      case 'cancelled':
        return Icons.block;
      case 'pendingmanager':
      case 'pendinghr':
      case 'pending':
        return Icons.hourglass_empty;
      default:
        return Icons.info;
    }
  }

  String _formatStatus(String status) {
    return status.replaceAllMapped(
      RegExp(r'([a-z])([A-Z])'),
          (match) => '${match.group(1)} ${match.group(2)}',
    );
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;
  final String? subtitle;

  const _SectionHeader({required this.title, this.subtitle});

  @override
  Widget build(BuildContext context) {
    return Text(
      subtitle ?? title,
      style: Theme.of(context).textTheme.titleMedium?.copyWith(
        fontWeight: FontWeight.w600,
      ),
    );
  }
}

class _ReadOnlyContent extends StatelessWidget {
  final String value;
  final IconData icon;
  final bool isDark;
  final bool multiline;

  const _ReadOnlyContent({
    required this.value,
    required this.icon,
    required this.isDark,
    this.multiline = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment:
      multiline ? CrossAxisAlignment.start : CrossAxisAlignment.center,
      children: [
        Icon(
          icon,
          color: AppColors.primary,
          size: 20,
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 14,
              color: isDark ? Colors.white : Colors.black87,
            ),
          ),
        ),
      ],
    );
  }
}

class _DateCard extends StatelessWidget {
  final String label;
  final String date;
  final bool isDark;

  const _DateCard({
    required this.label,
    required this.date,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isDark ? Colors.grey.shade800 : Colors.white,
        borderRadius: BorderRadius.circular(8),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              color: Colors.grey.shade600,
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            date,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}