import 'package:flutter/material.dart';

extension WidgetExtensions on Widget {
  Widget clipRRect(
    double radius, {
    double width = 50,
    double height = 50,
    BoxFit fit = BoxFit.cover,
  }) {
    return ClipRRect(borderRadius: BorderRadius.circular(radius), child: this);
  }
}
