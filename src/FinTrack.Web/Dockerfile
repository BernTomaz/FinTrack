FROM node:24-alpine

# shortcut: npm 11.21.1 inclui Undici vulneravel; remover o ajuste quando o npm incorporar 6.29.1 ou superior.
RUN npm install --global npm@11.21.1 \
    && npm install --prefix /tmp/fintrack-npm-patch --ignore-scripts --no-audit --no-fund undici@6.29.1 \
    && cp -a /tmp/fintrack-npm-patch/node_modules/undici/. /usr/local/lib/node_modules/npm/node_modules/undici/ \
    && rm -rf /tmp/fintrack-npm-patch

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

EXPOSE 4200

CMD ["npm", "start", "--", "--host", "0.0.0.0", "--port", "4200"]
