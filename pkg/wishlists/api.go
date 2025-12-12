package wishlists

import (
	"net/http"

	"github.com/nats-io/nats.go"
	"github.com/spf13/viper"
)

func Create(w http.ResponseWriter, req *http.Request) {
	w.Write([]byte("TODO lol"))
}

func Get(w http.ResponseWriter, req *http.Request) {
	conn, err := nats.Connect(viper.GetString("nats"))
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer conn.Drain()

	w.Write([]byte(conn.ConnectedServerName()))
}

func List(w http.ResponseWriter, req *http.Request) {
	conn, err := nats.Connect(viper.GetString("nats"))
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer conn.Close()

	w.Write([]byte(conn.ConnectedUrl()))
}
