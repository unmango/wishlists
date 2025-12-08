package main

import (
	"github.com/nats-io/nats.go"
	"github.com/unmango/go/cli"
)

func main() {
	if nc, err := nats.Connect(nats.DefaultURL); err != nil {
		cli.Fail(err)
	} else {
		println("Hello World!", nc.Statistics.Reconnects)
	}
}
