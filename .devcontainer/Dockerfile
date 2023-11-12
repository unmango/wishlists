FROM archlinux:base-20231015.0.185077

ARG USERNAME=vscode
ARG UID=1000
ARG GID=$UID

RUN groupadd --gid $GID $USERNAME \
	&& useradd --uid $UID --gid $GID -m $USERNAME \
	&& pacman -Syu --needed --noconfirm \
		inetutils \
		sudo \
		zsh \
		base-devel \
		git \
		go \
	&& echo "$USERNAME ALL=(root) NOPASSWD:ALL" > /etc/sudoers.d/$USERNAME \
	&& chmod 0440 /etc/sudoers.d/$USERNAME

USER $USERNAME

WORKDIR /home/$USERNAME
RUN git clone https://aur.archlinux.org/yay.git \
	&& cd yay && makepkg -sri --needed --noconfirm \
	&& rm -rf .cache yay

COPY .nvmrc .
ENV NVM_DIR=/usr/share/nvm

RUN yay -Sy --noconfirm \
	docker \
	docker-compose \
	python \
	thefuck \
	nvm \
	dotnet-sdk-preview-bin \
	azure-cli-bin \
	&& nvm install && nvm use

ENV PATH=$NVM_DIR:$PATH
WORKDIR /workspaces
