# base image
FROM node:12.2.0-alpine as build

# set working directory
WORKDIR /app

# add `/app/node_modules/.bin` to $PATH
ENV PATH /app/node_modules/.bin:$PATH

# install and cache app dependencies
COPY package.json /app/package.json
COPY tsconfig.json /app/tsconfig.json
COPY webpack.config.js /app/webpack.config.js
COPY webpack-prod.config.js /app/webpack-prod.config.js
COPY yarn.lock /app/yarn.lock

RUN npm config set unsafe-perm true
RUN yarn config set unsafe-perm true
RUN yarn config set registry http://npmjs.od.rferl.org:80/ -g
RUN yarn config set '@babel:registry' https://registry.npmjs.org/ -g
RUN yarn config set '@typescript-eslint:registry' https://registry.npmjs.org/ -g

COPY . /app
RUN yarn
RUN yarn build

# production environment
FROM nginx:1.16.0-alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]