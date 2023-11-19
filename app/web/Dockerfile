FROM node:21.2.0 as build

COPY package*.json .
RUN npm ci

COPY ./ .
RUN npm run build

FROM nginx:1.25.3
COPY --from=build /build /usr/share/nginx/html
