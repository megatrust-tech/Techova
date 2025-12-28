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

/// Leave review screen for Manager and HR to approve/reject leave requests
class LeaveReviewScreen extends StatefulWidget {
  final LeaveItem leave;
  final String displayName;
  /// 'manager' or 'hr' - determines which API endpoint to use
  final String reviewerRole;

  const LeaveReviewScreen({
    super.key,
    required this.leave,
    required this.displayName,
    required this.reviewerRole,
  });

  @override
  State<LeaveReviewScreen> createState() => _LeaveReviewScreenState();
}

class _LeaveReviewScreenState extends State<LeaveReviewScreen> {
  final _commentController = TextEditingController();
  bool _isSubmitting = false;
  bool _isDownloadingAttachment = false;

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final l10n = AppLocalizations.of(context)!;

    // Shared decoration matching NewLeaveRequestScreen and LeaveDetailsScreen
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
        title: const Text('Review Request'),
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
            // Employee info header
            _buildEmployeeHeader(isDark),
            const SizedBox(height: 24),

            // Status badge
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
              _SectionHeader(title: l10n.notes),
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

            // Comment field (editable)
            const _SectionHeader(title: 'Comment', optional: true),
            const SizedBox(height: 12),
            TextField(
              controller: _commentController,
              maxLines: 3,
              decoration: baseInputDecoration.copyWith(
                hintText: 'Add a comment for the employee...',
              ),
            ),
            const SizedBox(height: 32),

            // Approve/Reject buttons
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: _isSubmitting ? null : () => _submitAction(false),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      side: BorderSide(color: AppColors.error),
                      foregroundColor: AppColors.error,
                    ),
                    child: _isSubmitting
                        ? SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: AppColors.error,
                            ),
                          )
                        : const Text('Reject'),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  flex: 2,
                  child: ElevatedButton(
                    onPressed: _isSubmitting ? null : () => _submitAction(true),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.success,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      disabledBackgroundColor:
                          AppColors.success.withValues(alpha: 0.3),
                    ),
                    child: _isSubmitting
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Text('Approve'),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildEmployeeHeader(bool isDark) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
      decoration: BoxDecoration(
        color: AppColors.primary.withValues(alpha: 0.1),
        border: Border.all(
          color: Colors.grey.withValues(alpha: 0.3),
        ),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(
                widget.displayName.isNotEmpty
                    ? widget.displayName[0].toUpperCase()
                    : 'E',
                style: TextStyle(
                  color: AppColors.primary,
                  fontWeight: FontWeight.bold,
                  fontSize: 20,
                ),
              ),
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  widget.displayName,
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 16,
                    color: isDark ? Colors.white : Colors.black87,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  widget.leave.employeeEmail ?? '',
                  style: TextStyle(
                    fontSize: 13,
                    color: Colors.grey.shade600,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
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
      
      final response = await dioClient.get(
        '/leaves/${widget.leave.id}/attachment',
        options: Options(responseType: ResponseType.bytes),
      );

      String filename = 'attachment_${widget.leave.id}';
      final contentDisposition = response.headers['content-disposition'];
      if (contentDisposition != null && contentDisposition.isNotEmpty) {
        final header = contentDisposition.first;
        if (header.contains('filename=')) {
          filename = header.split('filename=').last.replaceAll('"', '').replaceAll("'", '');
        }
      }

      if (!filename.contains('.')) {
        final contentType = response.headers['content-type']?.first ?? '';
        final ext = _getExtensionFromContentType(contentType);
        if (ext.isNotEmpty) {
          filename = '$filename.$ext';
        }
      }

      final tempDir = await getTemporaryDirectory();
      final filePath = '${tempDir.path}/$filename';
      final file = File(filePath);
      await file.writeAsBytes(response.data);

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
        }
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message), backgroundColor: AppColors.error),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: ${e.toString()}'), backgroundColor: AppColors.error),
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
    if (type.contains('msword')) return 'doc';
    if (type.contains('wordprocessingml')) return 'docx';
    return '';
  }

  Future<void> _submitAction(bool isApproved) async {
    setState(() => _isSubmitting = true);

    try {
      final dioClient = sl<DioClient>();
      
      // Determine which endpoint to use based on reviewer role
      final endpoint = widget.reviewerRole == 'hr'
          ? ApiEndpoints.hrAction(widget.leave.id)
          : ApiEndpoints.managerAction(widget.leave.id);

      await dioClient.post(
        endpoint,
        data: {
          'isApproved': isApproved,
          'comment': _commentController.text.trim(),
        },
      );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              isApproved
                  ? 'Leave request approved successfully'
                  : 'Leave request rejected',
            ),
            backgroundColor: isApproved ? AppColors.success : AppColors.error,
          ),
        );
        Navigator.of(context).pop(true); // Return true to indicate refresh needed
      }
    } on DioException catch (e) {
      if (mounted) {
        String message = isApproved
            ? 'Failed to approve leave request'
            : 'Failed to reject leave request';
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
        setState(() => _isSubmitting = false);
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
  final bool optional;

  const _SectionHeader({
    required this.title,
    this.optional = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(
          title,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        if (optional) ...[
          const SizedBox(width: 4),
          Text(
            '(Optional)',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: Colors.grey,
            ),
          ),
        ],
      ],
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
