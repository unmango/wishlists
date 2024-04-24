FROM node:21.7.3 as build

WORKDIR /build
COPY package*.json .
RUN npm ci

COPY ./ .
RUN npm run build

FROM nginx:1.26.0
COPY --from=build /build/dist /usr/share/nginx/html
