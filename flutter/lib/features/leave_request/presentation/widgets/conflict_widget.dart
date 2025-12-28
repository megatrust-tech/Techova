import 'package:flutter/material.dart';

/// Widget displaying a conflict warning message
class ConflictWidget extends StatelessWidget {
  final String message;
  const ConflictWidget({super.key, required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xffe8585c).withValues(alpha: 0.11),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: const Color(0xffe8585c),
          width: 1.5,
        ),
      ),
      child: Column(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: const BoxDecoration(
              color: Color(0xfffee2e2),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.warning_rounded,
              color: Color(0xffe8585c),
              size: 24,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'A Conflict Occurred',
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w600,
              color: Theme.of(context).brightness == Brightness.dark
                  ? const Color(0xffd2cfd3)
                  : const Color(0xff4a4a4a),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            message,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w400,
              color: Theme.of(context).brightness == Brightness.dark
                  ? const Color(0xffd2cfd3)
                  : const Color(0xff6a6a6a),
            ),
          ),
        ],
      ),
    );
  }
}
