FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG VITE_RABBITMQ_MANAGEMENT_URL=http://localhost:15672
ARG VITE_RABBITMQ_USERNAME=admin
ARG VITE_RABBITMQ_PASSWORD=admin
ENV VITE_RABBITMQ_MANAGEMENT_URL=$VITE_RABBITMQ_MANAGEMENT_URL \
    VITE_RABBITMQ_USERNAME=$VITE_RABBITMQ_USERNAME \
    VITE_RABBITMQ_PASSWORD=$VITE_RABBITMQ_PASSWORD

RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
