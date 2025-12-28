import 'package:flutter/material.dart';
import '../../../../core/utiles/app_colors.dart';

class CustomCard extends StatelessWidget {
  const CustomCard({
    super.key,
    this.radius = 16,
    required this.child,
    this.borderColor,
    this.backgroundColor,
  });

  final double radius;
  final Widget child;
  final Color? borderColor;
  final Color? backgroundColor;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      decoration: BoxDecoration(
        color: backgroundColor ?? AppColors.surfaceColor,
        borderRadius: BorderRadius.circular(radius),
        border: Border.all(
          color: borderColor ??
              (isDark ? Colors.white.withOpacity(0.06) : Colors.black.withOpacity(0.06)),
        ),
      ),
      child: child,
    );
  }
}
