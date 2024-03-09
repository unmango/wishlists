FROM node:21.7.0 as build

WORKDIR /build
COPY package*.json .
RUN npm ci

COPY ./ .
RUN npm run build

FROM nginx:1.25.4
COPY --from=build /build/dist /usr/share/nginx/html
