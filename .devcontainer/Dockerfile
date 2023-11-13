FROM archlinux:base-20231015.0.185077

ARG USERNAME=vscode
ARG UID=1000
ARG GID=$UID

RUN groupadd --gid $GID $USERNAME \
	&& useradd --uid $UID --gid $GID -m $USERNAME \
	&& pacman -Syu --needed --noconfirm \
		inetutils \
		sudo \
		bash \
		zsh \
		base-devel \
		curl \
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
ENV NVM_DIR=~/.nvm

RUN yay -Sy --noconfirm \
	docker \
	docker-compose \
	python \
	thefuck \
	dotnet-sdk-preview-bin \
	azure-cli-bin \
	&& curl https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.5/install.sh | bash \
	&& . $NVM_DIR/nvm.sh \
	&& nvm install && nvm use

ENV PATH=$NVM_DIR:$PATH
WORKDIR /workspaces
