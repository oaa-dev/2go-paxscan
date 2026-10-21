# PHP 8.5 dev image for PaxScan Online (Laravel). Extensions Laravel + PostgreSQL need.
FROM php:8.5-cli

RUN apt-get update \
 && apt-get install -y --no-install-recommends git unzip libpq-dev libicu-dev libzip-dev \
 && docker-php-ext-install -j"$(nproc)" pdo_pgsql intl zip bcmath pcntl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

# Run as the host user so files created in the bind mount are not owned by root.
ARG UID=1000
ARG GID=1000
RUN groupadd -g "${GID}" app && useradd -m -u "${UID}" -g "${GID}" app
USER app
RUN mkdir -p /home/app/.composer
ENV COMPOSER_HOME=/home/app/.composer

WORKDIR /var/www/backend
