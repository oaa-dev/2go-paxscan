<?php

use Illuminate\Auth\Access\AuthorizationException;
use Illuminate\Auth\AuthenticationException;
use Illuminate\Database\Eloquent\ModelNotFoundException;
use Illuminate\Foundation\Application;
use Illuminate\Foundation\Configuration\Exceptions;
use Illuminate\Foundation\Configuration\Middleware;
use Illuminate\Http\Exceptions\ThrottleRequestsException;
use Illuminate\Http\Request;
use Illuminate\Validation\ValidationException;
use Symfony\Component\HttpKernel\Exception\AccessDeniedHttpException;
use Symfony\Component\HttpKernel\Exception\HttpExceptionInterface;
use Symfony\Component\HttpKernel\Exception\NotFoundHttpException;

return Application::configure(basePath: dirname(__DIR__))
    ->withRouting(
        web: __DIR__.'/../routes/web.php',
        api: __DIR__.'/../routes/api.php',
        commands: __DIR__.'/../routes/console.php',
        health: '/up',
    )
    ->withMiddleware(function (Middleware $middleware): void {
        //
    })
    ->withExceptions(function (Exceptions $exceptions): void {
        $exceptions->shouldRenderJsonWhen(
            fn (Request $request) => $request->is('api/*') || $request->expectsJson(),
        );

        // API errors use the same {success, message, errors?} envelope as successes.
        $exceptions->render(function (Throwable $e, Request $request) {
            if (! $request->is('api/*')) {
                return null;
            }

            [$status, $message, $errors] = match (true) {
                $e instanceof ValidationException => [422, $e->getMessage(), $e->errors()],
                $e instanceof AuthenticationException => [401, 'Unauthenticated.', null],
                $e instanceof AuthorizationException, $e instanceof AccessDeniedHttpException => [403, $e->getMessage() ?: 'Forbidden.', null],
                $e instanceof ModelNotFoundException, $e instanceof NotFoundHttpException => [404, 'Not found.', null],
                $e instanceof ThrottleRequestsException => [429, 'Too many requests.', null],
                $e instanceof HttpExceptionInterface => [$e->getStatusCode(), $e->getMessage() ?: 'Request failed.', null],
                default => [null, null, null],
            };

            if ($status === null) {
                return null; // Let Laravel render (and report) unexpected errors.
            }

            $headers = $e instanceof HttpExceptionInterface ? $e->getHeaders() : [];

            return response()->json(array_filter(
                ['success' => false, 'message' => $message, 'errors' => $errors],
                fn ($value) => $value !== null,
            ), $status, $headers);
        });
    })->create();
