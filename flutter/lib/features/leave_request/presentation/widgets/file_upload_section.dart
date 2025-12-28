import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../../../../core/utiles/app_colors.dart';

/// Widget for selecting and displaying an attached file
class FileUploadSection extends StatelessWidget {
  final PlatformFile? selectedFile;
  final VoidCallback onSelectFile;
  final VoidCallback onRemoveFile;

  /// Allowed file extensions
  static const List<String> allowedExtensions = [
    'jpg',
    'jpeg',
    'png',
    'pdf',
    'doc',
    'docx',
  ];

  const FileUploadSection({
    super.key,
    required this.selectedFile,
    required this.onSelectFile,
    required this.onRemoveFile,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    if (selectedFile != null) {
      return _SelectedFileCard(
        file: selectedFile!,
        onRemove: onRemoveFile,
        isDark: isDark,
      );
    }

    return InkWell(
      onTap: onSelectFile,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: isDark ? Colors.grey.shade900 : Colors.grey.shade100,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: Colors.grey.withValues(alpha: 0.3),
            width: 1.5,
          ),
        ),
        child: Column(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(
                Icons.cloud_upload_outlined,
                color: AppColors.primary,
                size: 24,
              ),
            ),
            const SizedBox(height: 12),
            Text(
              'Upload Attachment',
              style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              'JPG, PNG, PDF, DOC, DOCX',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurface.withValues(alpha: 0.5),
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Pick a file with allowed extensions
  static Future<PlatformFile?> pickFile() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: allowedExtensions,
        allowMultiple: false,
      );

      if (result != null && result.files.isNotEmpty) {
        return result.files.first;
      }
    } catch (e) {
      debugPrint('Error picking file: $e');
    }
    return null;
  }
}

class _SelectedFileCard extends StatelessWidget {
  final PlatformFile file;
  final VoidCallback onRemove;
  final bool isDark;

  const _SelectedFileCard({
    required this.file,
    required this.onRemove,
    required this.isDark,
  });

  IconData get _fileIcon {
    final extension = file.extension?.toLowerCase() ?? '';
    switch (extension) {
      case 'pdf':
        return Icons.picture_as_pdf;
      case 'doc':
      case 'docx':
        return Icons.description;
      case 'jpg':
      case 'jpeg':
      case 'png':
        return Icons.image;
      default:
        return Icons.insert_drive_file;
    }
  }

  String get _fileSize {
    final bytes = file.size;
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isDark ? Colors.grey.shade900 : Colors.grey.shade100,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: AppColors.primary.withValues(alpha: 0.5),
          width: 1.5,
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(
              _fileIcon,
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
                  file.name,
                  style: const TextStyle(
                    fontWeight: FontWeight.w500,
                    fontSize: 14,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Text(
                  _fileSize,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey.shade600,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            onPressed: onRemove,
            icon: const Icon(Icons.close, size: 20),
            style: IconButton.styleFrom(
              foregroundColor: Colors.grey.shade600,
            ),
          ),
        ],
      ),
    );
  }
}
