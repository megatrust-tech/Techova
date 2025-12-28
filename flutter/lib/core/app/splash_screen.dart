import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/auth/presentation/screen/login_screen.dart';
import '../layout/main_layout.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  bool _navigated = false;

  void _go(Widget page) {
    if (_navigated) return;
    _navigated = true;

    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => page),
          (_) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return BlocListener<AuthBloc, AuthState>(
      listenWhen: (prev, curr) =>
      curr is Authenticated || curr is Unauthenticated,
      listener: (context, state) async {
        await Future.delayed(const Duration(milliseconds: 900));
        if (!mounted) return;

        if (state is Authenticated) {
          _go(const MainLayout());
        } else if (state is Unauthenticated) {
          _go(const LoginScreen());
        }
      },
      child: const Scaffold(
        body: Center(
          child: Image(
            image: AssetImage('assets/images/logo.png'),
            width: 260,
            height: 260,
            fit: BoxFit.contain,
          ),
        ),
      ),
    );
  }
}
