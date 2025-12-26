import 'package:flutter/material.dart';
import 'core/app/app.dart';
import 'core/servicelocator/servicelocator.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize Dependency Injection
  await initDependencies();

  runApp(const LeaveApp());
}