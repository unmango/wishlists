FROM node:20.9.0 as build

WORKDIR /build
COPY package*.json .
RUN npm ci

COPY ./ .
RUN npm run build

FROM nginx:1.25.3
COPY --from=build /build/build /usr/share/nginx/html
